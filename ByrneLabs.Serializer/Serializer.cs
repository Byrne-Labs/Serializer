using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;

namespace ByrneLabs.Serializer
{
    internal enum RecordType : byte
    {
        Array = 1,
        Field = 2,
        KnownTypeObject = 3,
        Object = 4,
        String = 5,
        Type = 6
    }

    internal class Serializer
    {
        internal static readonly IDictionary<Type, uint> RecognizedTypes = new Dictionary<Type, uint> { { typeof(bool), 1 }, { typeof(char), 2 }, { typeof(byte), 3 }, { typeof(short), 4 }, { typeof(int), 5 }, { typeof(long), 6 }, { typeof(ushort), 7 }, { typeof(uint), 8 }, { typeof(ulong), 9 }, { typeof(float), 10 }, { typeof(double), 11 }, { typeof(DateTime), 12 }, { typeof(Guid), 13 }, { typeof(string), ushort.MaxValue - 1 } };
        private readonly IDictionary<(Type, FieldInfo), uint> _fieldsIndex = new Dictionary<(Type, FieldInfo), uint>();
        private readonly Type[] _knownTypes = { typeof(bool), typeof(char), typeof(byte), typeof(short), typeof(int), typeof(long), typeof(ushort), typeof(uint), typeof(ulong), typeof(float), typeof(double), typeof(DateTime), typeof(string), typeof(Guid) };
        private readonly Stack<(object, uint)> _objectsToSerialize = new Stack<(object, uint)>();
        private BinaryWriter _serializedArrays;
        private BinaryWriter _serializedFields;
        private BinaryWriter _serializedKnownTypeObjects;
        private BinaryWriter _serializedObjects;
        private readonly ObjectIndex _serializedObjectsIndex = new ObjectIndex();
        private BinaryWriter _serializedStrings;
        private BinaryWriter _serializedTypes;
        private readonly IDictionary<uint, FieldInfo[]> _typeFields = new Dictionary<uint, FieldInfo[]>();
        private readonly IDictionary<Type, uint> _typeIndex = new Dictionary<Type, uint>();
        private uint _nextId = ushort.MaxValue;

        public byte[] Serialize(object obj)
        {
            using (var memoryStream = new MemoryStream())
            {
                Serialize(memoryStream, obj);
                return memoryStream.ToArray();
            }
        }

        public void Serialize(Stream serializationStream, object obj)
        {
            var arraysMemoryStream = new MemoryStream();
            var fieldsMemoryStream = new MemoryStream();
            var knownTypeObjectsMemoryStream = new MemoryStream();
            var objectsMemoryStream = new MemoryStream();
            var stringsMemoryStream = new MemoryStream();
            var typesMemoryStream = new MemoryStream();

            _serializedArrays = new BinaryWriter(arraysMemoryStream);
            _serializedFields = new BinaryWriter(fieldsMemoryStream);
            _serializedKnownTypeObjects = new BinaryWriter(knownTypeObjectsMemoryStream);
            _serializedObjects = new BinaryWriter(objectsMemoryStream);
            _serializedStrings = new BinaryWriter(stringsMemoryStream);
            _serializedTypes = new BinaryWriter(typesMemoryStream);

            var rootObjectId = GetObjectId(obj);

            while (_objectsToSerialize.Count > 0)
            {
                var (objectToDeserialize, objectId) = _objectsToSerialize.Pop();
                SerializeObject(objectToDeserialize, objectId);
            }

            using (var binaryWriter = new BinaryWriter(serializationStream, Encoding.UTF8, true))
            {
                binaryWriter.Write(rootObjectId);
                binaryWriter.Write(typesMemoryStream.ToArray());
                binaryWriter.Write(fieldsMemoryStream.ToArray());
                binaryWriter.Write(stringsMemoryStream.ToArray());
                binaryWriter.Write(knownTypeObjectsMemoryStream.ToArray());
                binaryWriter.Write(objectsMemoryStream.ToArray());
                binaryWriter.Write(arraysMemoryStream.ToArray());
            }
        }

        private uint GetObjectId(object obj)
        {
            if (obj == null)
            {
                return 0;
            }

            if (_serializedObjectsIndex.TryGetId(obj, out var objectId))
            {
                return objectId;
            }

            objectId = _nextId++;
            _serializedObjectsIndex.Add(obj, objectId);
            _objectsToSerialize.Push((obj, objectId));
            return objectId;
        }

        private uint GetTypeId(Type type, Type originalType)
        {
            if (_typeIndex.TryGetValue(type, out var typeId))
            {
                return typeId;
            }

            if (type == originalType)
            {
                var constructedType = type;
                while (constructedType.DeclaringType != null)
                {
                    constructedType = constructedType.DeclaringType;
                }

                if (!_knownTypes.Contains(constructedType) && constructedType.GetConstructor(Array.Empty<Type>()) == null)
                {
                    throw new ArgumentException($"The type {type.FullName} does not have a default constructor");
                }
            }

            typeId = _nextId++;
            _typeIndex.Add(type, typeId);

            var assemblyQualifiedNameBytes = Encoding.UTF8.GetBytes(type.AssemblyQualifiedName);
            _serializedTypes.Write((byte)RecordType.Type);
            _serializedTypes.Write(typeId);
            _serializedTypes.Write(assemblyQualifiedNameBytes.Length);
            _serializedTypes.Write(assemblyQualifiedNameBytes);

            var fields = new List<FieldInfo>();
            var baseType = type;
            while (baseType != null)
            {
                fields.AddRange(baseType.GetFields(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.DeclaredOnly).Where(f => !f.IsNotSerialized));

                baseType = baseType.BaseType;
            }

            _typeFields.Add(typeId, fields.ToArray());
            foreach (var field in fields)
            {
                var fieldId = _nextId++;
                _fieldsIndex.Add((type, field), fieldId);

                _serializedFields.Write((byte)RecordType.Field);
                _serializedFields.Write(fieldId);
                _serializedFields.Write(typeId);
                var fieldNameBytes = Encoding.UTF8.GetBytes(field.Name);
                _serializedFields.Write(fieldNameBytes.Length);
                _serializedFields.Write(fieldNameBytes);
            }

            if (type.BaseType != null)
            {
                GetTypeId(type.BaseType, originalType);
            }

            return typeId;
        }

        private void SerializeObject(object obj, uint objectId)
        {
            if (obj is string objString)
            {
                _serializedStrings.Write((byte)RecordType.String);
                _serializedStrings.Write(objectId);
                var stringBytes = Encoding.UTF8.GetBytes(objString);
                _serializedStrings.Write(stringBytes.Length);
                _serializedStrings.Write(stringBytes);
            }
            else if (obj.GetType().IsArray)
            {
                _serializedArrays.Write((byte)RecordType.Array);
                _serializedArrays.Write(objectId);
                var elementType = obj.GetType().GetElementType();
                _serializedArrays.Write(GetTypeId(elementType, elementType));
                var array = (Array)obj;
                _serializedArrays.Write(array.Length);
                for (long index = 0; index < array.Length; index++)
                {
                    var itemValue = array.GetValue(index);
                    _serializedArrays.Write(GetObjectId(itemValue));
                }
            }
            else if (Array.IndexOf(_knownTypes, obj.GetType()) >= 0)
            {
                byte[] bytes;
                switch (obj)
                {
                    case bool boolObj:
                        bytes = BitConverter.GetBytes(boolObj);
                        break;
                    case char charObj:
                        bytes = BitConverter.GetBytes(charObj);
                        break;
                    case byte byteObj:
                        bytes = new[] { byteObj };
                        break;
                    case short shortObj:
                        bytes = BitConverter.GetBytes(shortObj);
                        break;
                    case int intObj:
                        bytes = BitConverter.GetBytes(intObj);
                        break;
                    case long longObj:
                        bytes = BitConverter.GetBytes(longObj);
                        break;
                    case ushort ushortObj:
                        bytes = BitConverter.GetBytes(ushortObj);
                        break;
                    case uint uintObj:
                        bytes = BitConverter.GetBytes(uintObj);
                        break;
                    case ulong ulongObj:
                        bytes = BitConverter.GetBytes(ulongObj);
                        break;
                    case float floatObj:
                        bytes = BitConverter.GetBytes(floatObj);
                        break;
                    case double doubleObj:
                        bytes = BitConverter.GetBytes(doubleObj);
                        break;
                    case DateTime dateTimeObj:
                        bytes = BitConverter.GetBytes(dateTimeObj.Ticks);
                        break;
                    case Guid guidObj:
                        bytes = guidObj.ToByteArray();
                        break;
                    default:
                        throw new ArgumentException($"Unexpected type {obj.GetType().FullName}");
                }

                var typeId = GetTypeId(obj.GetType(), obj.GetType());

                _serializedKnownTypeObjects.Write((byte)RecordType.KnownTypeObject);
                _serializedKnownTypeObjects.Write(objectId);
                _serializedKnownTypeObjects.Write(typeId);
                _serializedKnownTypeObjects.Write(bytes.Length);
                _serializedKnownTypeObjects.Write(bytes);
            }
            else
            {
                var typeId = GetTypeId(obj.GetType(), obj.GetType());
                _serializedObjects.Write((byte)RecordType.Object);
                _serializedObjects.Write(objectId);
                _serializedObjects.Write(typeId);
                var fields = _typeFields[typeId];
                _serializedObjects.Write(fields.Length);
                foreach (var field in fields)
                {
                    var fieldValue = field.GetValue(obj);
                    var fieldId = _fieldsIndex[(obj.GetType(), field)];
                    _serializedObjects.Write(fieldId);
                    _serializedObjects.Write(GetObjectId(fieldValue));
                }
            }
        }
    }
}
