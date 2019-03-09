using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;

namespace ByrneLabs.Serializer
{
    internal class Serializer
    {
        internal static readonly Dictionary<Type, uint> RecognizedTypes = new Dictionary<Type, uint> { { typeof(bool), 1 }, { typeof(char), 2 }, { typeof(byte), 3 }, { typeof(short), 4 }, { typeof(int), 5 }, { typeof(long), 6 }, { typeof(ushort), 7 }, { typeof(uint), 8 }, { typeof(ulong), 9 }, { typeof(float), 10 }, { typeof(double), 11 }, { typeof(DateTime), 12 }, { typeof(Guid), 13 }, { typeof(string), ushort.MaxValue - 1 } };
        private readonly Dictionary<(Type, FieldInfo), uint> _fieldsIndex = new Dictionary<(Type, FieldInfo), uint>();
        private readonly Type[] _knownTypes = { typeof(bool), typeof(char), typeof(byte), typeof(short), typeof(int), typeof(long), typeof(ushort), typeof(uint), typeof(ulong), typeof(float), typeof(double), typeof(DateTime), typeof(string), typeof(Guid) };
        private readonly Stack<(object, uint)> _objectsToSerialize = new Stack<(object, uint)>();
        private readonly Dictionary<uint, byte[]> _serializedArrays = new Dictionary<uint, byte[]>();
        private readonly Dictionary<uint, byte[]> _serializedFields = new Dictionary<uint, byte[]>();
        private readonly Dictionary<uint, byte[]> _serializedKnownTypeObjects = new Dictionary<uint, byte[]>();
        private readonly Dictionary<uint, byte[]> _serializedObjects = new Dictionary<uint, byte[]>();
        private readonly Dictionary<object, uint> _serializedObjectsIndex = new Dictionary<object, uint>();
        private readonly Dictionary<uint, byte[]> _serializedStrings = new Dictionary<uint, byte[]>();
        private readonly Dictionary<uint, byte[]> _serializedTypes = new Dictionary<uint, byte[]>();
        private readonly Dictionary<uint, FieldInfo[]> _typeFields = new Dictionary<uint, FieldInfo[]>();
        private readonly Dictionary<Type, uint> _typeIndex = new Dictionary<Type, uint>();
        private uint _nextId = ushort.MaxValue;

        private static void WriteSerializedItems(BinaryWriter binaryWriter, Dictionary<uint, byte[]> items)
        {
            binaryWriter.Write(items.Count);
            foreach (var item in items)
            {
                binaryWriter.Write(item.Key);
                binaryWriter.Write(item.Value);
            }
        }

        public byte[] Serialize(object obj)
        {
            var rootObjectId = GetObjectId(obj);

            while (_objectsToSerialize.Count > 0)
            {
                var (objectToDeserialize, objectId) = _objectsToSerialize.Pop();
                SerializeObject(objectToDeserialize, objectId);
            }

            using (var memoryStream = new MemoryStream(200))
            using (var binaryWriter = new BinaryWriter(memoryStream))
            {
                binaryWriter.Write(rootObjectId);
                WriteSerializedItems(binaryWriter, _serializedTypes);
                WriteSerializedItems(binaryWriter, _serializedFields);
                WriteSerializedItems(binaryWriter, _serializedStrings);
                WriteSerializedItems(binaryWriter, _serializedKnownTypeObjects);
                WriteSerializedItems(binaryWriter, _serializedObjects);
                WriteSerializedItems(binaryWriter, _serializedArrays);

                return memoryStream.ToArray();
            }
        }

        private uint GetObjectId(object obj)
        {
            if (obj == null)
            {
                return 0;
            }

            if (_serializedObjectsIndex.TryGetValue(obj, out var objectId))
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

            using (var memoryStream = new MemoryStream(400))
            using (var binaryWriter = new BinaryWriter(memoryStream))
            {
                var assemblyQualifiedNameBytes = Encoding.UTF8.GetBytes(type.AssemblyQualifiedName);
                binaryWriter.Write(assemblyQualifiedNameBytes.Length);
                binaryWriter.Write(assemblyQualifiedNameBytes);

                _serializedTypes.Add(typeId, memoryStream.ToArray());
            }

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

                using (var memoryStream = new MemoryStream(400))
                using (var binaryWriter = new BinaryWriter(memoryStream))
                {
                    binaryWriter.Write(typeId);
                    var fieldNameBytes = Encoding.UTF8.GetBytes(field.Name);
                    binaryWriter.Write(fieldNameBytes.Length);
                    binaryWriter.Write(fieldNameBytes);

                    _serializedFields.Add(fieldId, memoryStream.ToArray());
                }
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
                using (var memoryStream = new MemoryStream(200))
                using (var binaryWriter = new BinaryWriter(memoryStream))
                {
                    var stringBytes = Encoding.UTF8.GetBytes(objString);
                    binaryWriter.Write(stringBytes.Length);
                    binaryWriter.Write(stringBytes);

                    _serializedStrings.Add(objectId, memoryStream.ToArray());
                }
            }
            else if (obj.GetType().IsArray)
            {
                using (var memoryStream = new MemoryStream(200))
                using (var binaryWriter = new BinaryWriter(memoryStream))
                {
                    var elementType = obj.GetType().GetElementType();
                    binaryWriter.Write(GetTypeId(elementType, elementType));
                    var array = (Array) obj;
                    binaryWriter.Write(array.Length);
                    for (long index = 0; index < array.Length; index++)
                    {
                        var itemValue = array.GetValue(index);
                        binaryWriter.Write(GetObjectId(itemValue));
                    }

                    _serializedArrays.Add(objectId, memoryStream.ToArray());
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

                using (var memoryStream = new MemoryStream(20))
                using (var binaryWriter = new BinaryWriter(memoryStream))
                {
                    binaryWriter.Write(typeId);
                    binaryWriter.Write(bytes.Length);
                    binaryWriter.Write(bytes);
                    _serializedKnownTypeObjects.Add(objectId, memoryStream.ToArray());
                }
            }
            else
            {
                var typeId = GetTypeId(obj.GetType(), obj.GetType());
                using (var memoryStream = new MemoryStream(200))
                using (var binaryWriter = new BinaryWriter(memoryStream))
                {
                    binaryWriter.Write(typeId);
                    var fields = _typeFields[typeId];
                    binaryWriter.Write(fields.Length);
                    foreach (var field in fields)
                    {
                        var fieldValue = field.GetValue(obj);
                        var fieldId = _fieldsIndex[(obj.GetType(), field)];
                        binaryWriter.Write(fieldId);
                        binaryWriter.Write(GetObjectId(fieldValue));
                    }

                    _serializedObjects.Add(objectId, memoryStream.ToArray());
                }
            }
        }
    }
}
