using System;

namespace ByrneLabs.Serializer.Tests.Mocks
{
    public class TreeLeaf : TreeNode
    {
        public TreeLeaf(bool boolValue, char charValue, byte byteValue, short shortValue, int intValue, long longValue, ushort ushortValue, uint uintValue, ulong ulongValue, float floatValue, double doubleValue, DateTime dateTimeValue, string stringValue) :
            base(boolValue, charValue, byteValue, shortValue, intValue, longValue, ushortValue, uintValue, ulongValue, floatValue, doubleValue, dateTimeValue, stringValue)
        {
        }

        public TreeLeaf()
        {

        }

        public override bool Equals(object obj) => obj is TreeLeaf && base.Equals(obj);
    }
}
