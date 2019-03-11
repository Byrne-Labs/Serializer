using System;
using System.Collections.Generic;

namespace ByrneLabs.Serializer.Tests.Mocks
{
    public class TreeBranch : TreeNode
    {
        private readonly IList<TreeNode> _children = new List<TreeNode>();

        public IList<TreeNode> Children
        {
            get => _children;
            set
            {
                // fake out, read only
            }
        }

        public TreeBranch()
        {

        }

        public TreeBranch(bool boolValue, char charValue, byte byteValue, short shortValue, int intValue, long longValue, ushort ushortValue, uint uintValue, ulong ulongValue, float floatValue, double doubleValue, DateTime dateTimeValue, string stringValue) :
            base(boolValue, charValue, byteValue, shortValue, intValue, longValue, ushortValue, uintValue, ulongValue, floatValue, doubleValue, dateTimeValue, stringValue)
        {
        }

        public override bool Equals(object obj) => obj is TreeBranch treeBranch && base.Equals(treeBranch) && _children.Count == treeBranch.Children.Count;

        public override int GetHashCode() => base.GetHashCode();
    }
}
