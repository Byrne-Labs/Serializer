using System.Collections.Generic;
using System.Runtime.Serialization;

namespace ByrneLabs.Serializer
{
    public class ObjectIndex
    {
        private class InstanceComparer : IEqualityComparer<object>
        {
            bool IEqualityComparer<object>.Equals(object x, object y) => ReferenceEquals(x, y);

            int IEqualityComparer<object>.GetHashCode(object obj) => obj.GetHashCode();
        }

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
}
