using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
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

    [SuppressMessage("ReSharper", "ForCanBeConvertedToForeach")]
    internal class Serializer
    {
        private class ObjectIndex
        {
            private readonly ObjectIDGenerator _objectIdGenerator = new ObjectIDGenerator();
            private readonly SortedList<long, uint> _objectsIndex = new SortedList<long, uint>();

            public void Add(object obj, uint objectIndex)
            {
                var objectId = _objectIdGenerator.GetId(obj, out _);
                _objectsIndex.Add(objectId, objectIndex);
            }

            public bool TryGetId(object obj, out uint objectIndex)
            {
                var objectId = _objectIdGenerator.GetId(obj, out var firstTime);
                if (!firstTime)
                {
                    objectIndex = _objectsIndex[objectId];
                }
                else
                {
                    objectIndex = 0;
                }

                return !firstTime;
            }
        }

        private readonly ObjectIndex _binaryWriterIndex = new ObjectIndex();
        private readonly IDictionary<(Type, FieldInfo), uint> _fieldsIndex = new Dictionary<(Type, FieldInfo), uint>();
        private readonly Type[] _knownTypes = { typeof(bool), typeof(char), typeof(byte), typeof(short), typeof(int), typeof(long), typeof(ushort), typeof(uint), typeof(ulong), typeof(float), typeof(double), typeof(DateTime), typeof(Guid) };
        private readonly Stack<(object, uint)> _objectsToSerialize = new Stack<(object, uint)>();
        private readonly IDictionary<uint, FieldInfo[]> _typeFields = new Dictionary<uint, FieldInfo[]>();
        private readonly IDictionary<Type, uint> _typeIndex = new Dictionary<Type, uint>();
        private BinaryWriter _binaryWriter;
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
            _binaryWriter = new BinaryWriter(serializationStream, Encoding.UTF8, true);

            var rootObjectId = GetObjectId(obj);
            _binaryWriter.Write(rootObjectId);

            while (_objectsToSerialize.Count > 0)
            {
                var (objectToDeserialize, objectId) = _objectsToSerialize.Pop();
                SerializeObject(objectToDeserialize, objectId);
            }

            _binaryWriter.Close();
        }

        private uint GetObjectId(object obj)
        {
            if (obj == null)
            {
                return 0;
            }

            if (_binaryWriterIndex.TryGetId(obj, out var objectId))
            {
                return objectId;
            }

            objectId = _nextId++;
            _binaryWriterIndex.Add(obj, objectId);
            _objectsToSerialize.Push((obj, objectId));
            return objectId;
        }

        private uint GetTypeId(Type type, Type originalType)
        {
            if (_typeIndex.TryGetValue(type, out var typeId))
            {
                return typeId;
            }

            typeId = _nextId++;
            _typeIndex.Add(type, typeId);

            var assemblyQualifiedNameBytes = Encoding.UTF8.GetBytes(type.AssemblyQualifiedName);
            _binaryWriter.Write((byte) RecordType.Type);
            _binaryWriter.Write(typeId);
            _binaryWriter.Write(assemblyQualifiedNameBytes.Length);
            _binaryWriter.Write(assemblyQualifiedNameBytes);

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
                var fieldNameBytes = Encoding.UTF8.GetBytes(field.Name);

                _binaryWriter.Write((byte) RecordType.Field);
                _binaryWriter.Write(fieldId);
                _binaryWriter.Write(typeId);
                _binaryWriter.Write(fieldNameBytes.Length);
                _binaryWriter.Write(fieldNameBytes);
            }

            if (type.BaseType != null)
            {
                GetTypeId(type.BaseType, originalType);
            }

            return typeId;
        }

        private void SerializeObject(object obj, uint objectId)
        {
            var objectType = obj.GetType();
            if (obj is string objString)
            {
                var stringBytes = Encoding.UTF8.GetBytes(objString);
                _binaryWriter.Write((byte) RecordType.String);
                _binaryWriter.Write(objectId);
                _binaryWriter.Write(stringBytes.Length);
                _binaryWriter.Write(stringBytes);
            }
            else if (objectType.IsArray)
            {
                var elementType = objectType.GetElementType();
                var elementTypeObjectId = GetTypeId(elementType, elementType);
                var array = (Array) obj;
                var elementObjectIds = new uint[array.Length];
                for (long index = 0; index < array.Length; index++)
                {
                    var itemValue = array.GetValue(index);
                    var elementObjectId = GetObjectId(itemValue);
                    elementObjectIds.SetValue(elementObjectId, index);
                }

                _binaryWriter.Write((byte) RecordType.Array);
                _binaryWriter.Write(objectId);
                _binaryWriter.Write(elementTypeObjectId);
                _binaryWriter.Write(array.Length);
                for (long index = 0; index < array.Length; index++)
                {
                    _binaryWriter.Write(elementObjectIds[index]);
                }
            }
            else if (Array.IndexOf(_knownTypes, objectType) >= 0)
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
                        throw new ArgumentException($"Unexpected type {objectType.FullName}");
                }

                var typeId = GetTypeId(objectType, objectType);

                _binaryWriter.Write((byte) RecordType.KnownTypeObject);
                _binaryWriter.Write(objectId);
                _binaryWriter.Write(typeId);
                _binaryWriter.Write(bytes.Length);
                _binaryWriter.Write(bytes);
            }
            else
            {
                var typeId = GetTypeId(objectType, objectType);
                var fields = _typeFields[typeId];
                var fieldValueObjectIds = new uint[fields.Length];
                for (var index = 0; index < fields.Length; index++)
                {
                    var field = fields[index];
                    var fieldValue = field.GetValue(obj);
                    fieldValueObjectIds[index] = GetObjectId(fieldValue);
                }

                _binaryWriter.Write((byte) RecordType.Object);
                _binaryWriter.Write(objectId);
                _binaryWriter.Write(typeId);
                _binaryWriter.Write(fields.Length);
                for (var index = 0; index < fields.Length; index++)
                {
                    var field = fields[index];
                    var fieldId = _fieldsIndex[(objectType, field)];
                    _binaryWriter.Write(fieldId);
                    _binaryWriter.Write(fieldValueObjectIds[index]);
                }
            }
        }
    }
}
