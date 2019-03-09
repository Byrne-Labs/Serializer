using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;

namespace ByrneLabs.Serializer
{
    internal class Deserializer
    {
        private readonly IDictionary<uint, Array> _arrays = new Dictionary<uint, Array>();
        private readonly List<(Array, uint[])> _arraysToLoad = new List<(Array, uint[])>();
        private readonly IDictionary<uint, FieldInfo> _fields = new Dictionary<uint, FieldInfo>();
        private readonly IDictionary<uint, object> _knownTypeObjects = new Dictionary<uint, object>();
        private readonly IDictionary<uint, object> _objects = new Dictionary<uint, object>();
        private readonly List<(object, (uint, uint)[])> _objectToLoad = new List<(object, (uint, uint)[])>();
        private readonly IDictionary<uint, string> _strings = new Dictionary<uint, string>();
        private readonly IDictionary<uint, Type> _types = new Dictionary<uint, Type>();

        public object Deserialize(byte[] bytes)
        {
            uint rootObjectId;
            using (var memoryStream = new MemoryStream(bytes))
            using (var binaryReader = new BinaryReader(memoryStream))
            {
                rootObjectId = binaryReader.ReadUInt32();
                ReadTypes(binaryReader);
                ReadFields(binaryReader);
                ReadStrings(binaryReader);
                ReadKnownTypeObjects(binaryReader);
                ReadObjects(binaryReader);
                ReadArrays(binaryReader);
                if (memoryStream.Length != memoryStream.Position)
                {
                    throw new InvalidOperationException("All items have been read but there is more left in the byte stream");
                }
            }

            PopulateObjects();
            PopulateArrays();
            var rootObject = GetObject(rootObjectId);

            return rootObject;
        }

        private object GetObject(uint objectId)
        {
            object obj;
            if (objectId == 0)
            {
                obj = null;
            }
            else if (_knownTypeObjects.ContainsKey(objectId))
            {
                obj = _knownTypeObjects[objectId];
            }
            else if (_strings.ContainsKey(objectId))
            {
                obj = _strings[objectId];
            }
            else if (_objects.ContainsKey(objectId))
            {
                obj = _objects[objectId];
            }
            else if (_arrays.ContainsKey(objectId))
            {
                obj = _arrays[objectId];
            }
            else
            {
                throw new ArgumentException("Cannot find referenced object");
            }

            return obj;
        }

        private void PopulateArrays()
        {
            foreach (var (array, arrayElementObjectIds) in _arraysToLoad)
            {
                for (var arrayIndex = 0; arrayIndex < array.Length; arrayIndex++)
                {
                    var arrayElementObjectId = arrayElementObjectIds[arrayIndex];
                    var arrayElement = GetObject(arrayElementObjectId);
                    array.SetValue(arrayElement, arrayIndex);
                }
            }
        }

        private void PopulateObjects()
        {
            foreach (var (obj, fieldsToLoad) in _objectToLoad)
            {
                foreach (var (fieldId, fieldValueObjectId) in fieldsToLoad)
                {
                    var field = _fields[fieldId];
                    var fieldValue = GetObject(fieldValueObjectId);

                    field.SetValue(obj, fieldValue);
                }
            }
        }

        private void ReadArrays(BinaryReader binaryReader)
        {
            var arrayCount = binaryReader.ReadInt32();
            while (_arrays.Count < arrayCount)
            {
                var objectId = binaryReader.ReadUInt32();
                var elementTypeId = binaryReader.ReadUInt32();
                var itemCount = binaryReader.ReadInt32();
                var arrayElementsToLoad = new uint[itemCount];
                for (var itemIndex = 0; itemIndex < itemCount; itemIndex++)
                {
                    arrayElementsToLoad[itemIndex] = binaryReader.ReadUInt32();
                }

                var array = Array.CreateInstance(_types[elementTypeId], itemCount);
                _arraysToLoad.Add((array, arrayElementsToLoad));
                _arrays.Add(objectId, array);
            }
        }

        private void ReadFields(BinaryReader binaryReader)
        {
            var fieldCount = binaryReader.ReadInt32();
            while (_fields.Count < fieldCount)
            {
                var fieldId = binaryReader.ReadUInt32();
                var typeId = binaryReader.ReadUInt32();
                var fieldNameBytesLength = binaryReader.ReadInt32();
                var fieldNameBytes = binaryReader.ReadBytes(fieldNameBytesLength);
                var fieldName = Encoding.UTF8.GetString(fieldNameBytes);

                var type = _types[typeId];
                FieldInfo field = null;
                while (field == null && type != null)
                {
                    field = type.GetField(fieldName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                    type = type.BaseType;
                }

                if (field == null)
                {
                    throw new InvalidOperationException($"Could not find field {fieldName} on type {type.FullName}");
                }

                _fields.Add(fieldId, field);
            }
        }

        private void ReadKnownTypeObjects(BinaryReader binaryReader)
        {
            var knownTypeObjectCount = binaryReader.ReadInt32();
            while (_knownTypeObjects.Count < knownTypeObjectCount)
            {
                var objectId = binaryReader.ReadUInt32();
                var typeId = binaryReader.ReadUInt32();
                var objectBytesLength = binaryReader.ReadInt32();
                var objectBytes = binaryReader.ReadBytes(objectBytesLength);
                object obj;
                var type = _types[typeId];
                if (type == typeof(bool))
                {
                    obj = BitConverter.ToBoolean(objectBytes, 0);
                }
                else if (type == typeof(char))
                {
                    obj = BitConverter.ToChar(objectBytes, 0);
                }
                else if (type == typeof(byte))
                {
                    obj = objectBytes[0];
                }
                else if (type == typeof(short))
                {
                    obj = BitConverter.ToInt16(objectBytes, 0);
                }
                else if (type == typeof(int))
                {
                    obj = BitConverter.ToInt32(objectBytes, 0);
                }
                else if (type == typeof(long))
                {
                    obj = BitConverter.ToInt64(objectBytes, 0);
                }
                else if (type == typeof(ushort))
                {
                    obj = BitConverter.ToUInt16(objectBytes, 0);
                }
                else if (type == typeof(uint))
                {
                    obj = BitConverter.ToUInt32(objectBytes, 0);
                }
                else if (type == typeof(ulong))
                {
                    obj = BitConverter.ToUInt64(objectBytes, 0);
                }
                else if (type == typeof(float))
                {
                    obj = BitConverter.ToSingle(objectBytes, 0);
                }
                else if (type == typeof(double))
                {
                    obj = BitConverter.ToDouble(objectBytes, 0);
                }
                else if (type == typeof(DateTime))
                {
                    var ticks = BitConverter.ToInt64(objectBytes, 0);
                    obj = new DateTime(ticks);
                }
                else if (type == typeof(Guid))
                {
                    obj = new Guid(objectBytes);
                }
                else
                {
                    throw new ArgumentException($"Unexpected type {type.FullName}");
                }

                _knownTypeObjects.Add(objectId, obj);
            }
        }

        private void ReadObjects(BinaryReader binaryReader)
        {
            var objectCount = binaryReader.ReadInt32();
            while (_objects.Count < objectCount)
            {
                var objectId = binaryReader.ReadUInt32();
                var typeId = binaryReader.ReadUInt32();
                var fieldCount = binaryReader.ReadInt32();
                var obj = Activator.CreateInstance(_types[typeId]);

                var fieldsToLoad = new (uint, uint)[fieldCount];

                for (var fieldIndex = 0; fieldIndex < fieldCount; fieldIndex++)
                {
                    var fieldId = binaryReader.ReadUInt32();
                    var fieldValueObjectId = binaryReader.ReadUInt32();
                    fieldsToLoad[fieldIndex] = (fieldId, fieldValueObjectId);
                }

                _objectToLoad.Add((obj, fieldsToLoad));
                _objects.Add(objectId, obj);
            }
        }

        private void ReadStrings(BinaryReader binaryReader)
        {
            var stringCount = binaryReader.ReadInt32();
            while (_strings.Count < stringCount)
            {
                var objectId = binaryReader.ReadUInt32();
                var stringBytesLength = binaryReader.ReadInt32();
                var stringBytes = binaryReader.ReadBytes(stringBytesLength);
                var stringValue = Encoding.UTF8.GetString(stringBytes);

                _strings.Add(objectId, stringValue);
            }
        }

        private void ReadTypes(BinaryReader binaryReader)
        {
            var typeCount = binaryReader.ReadInt32();
            while (_types.Count < typeCount)
            {
                var typeId = binaryReader.ReadUInt32();
                var assemblyQualifiedNameBytesLength = binaryReader.ReadInt32();
                var assemblyQualifiedNameBytes = binaryReader.ReadBytes(assemblyQualifiedNameBytesLength);
                var assemblyQualifiedName = Encoding.UTF8.GetString(assemblyQualifiedNameBytes);
                var type = Type.GetType(assemblyQualifiedName);
                _types.Add(typeId, type);
            }
        }
    }
}
