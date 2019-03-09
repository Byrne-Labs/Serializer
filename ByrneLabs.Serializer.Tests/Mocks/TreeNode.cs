using System;
using System.Collections.Generic;
using ByrneLabs.Commons;

namespace ByrneLabs.Serializer.Tests.Mocks
{
    public class TreeNode
    {
        private readonly Dictionary<object, object> _attributes = new Dictionary<object, object>();
        private readonly bool _boolValue;
        private readonly byte _byteValue;
        private readonly char _charValue;
        private readonly IList<TreeNode> _children = new List<TreeNode>();
        private readonly DateTime _dateTimeValue;
        private readonly double _doubleValue;
        private readonly float _floatValue;
        private readonly int _intValue;
        private readonly long _longValue;
        private readonly short _shortValue;
        private readonly string _stringValue;
        private readonly uint _uintValue;
        private readonly ulong _ulongValue;
        private readonly ushort _ushortValue;

        public TreeNode()
        {
        }

        public TreeNode(bool boolValue, char charValue, byte byteValue, short shortValue, int intValue, long longValue, ushort ushortValue, uint uintValue, ulong ulongValue, float floatValue, double doubleValue, DateTime dateTimeValue, string stringValue)
        {
            _boolValue = boolValue;
            _charValue = charValue;
            _byteValue = byteValue;
            _shortValue = shortValue;
            _intValue = intValue;
            _longValue = longValue;
            _ushortValue = ushortValue;
            _uintValue = uintValue;
            _ulongValue = ulongValue;
            _floatValue = floatValue;
            _doubleValue = doubleValue;
            _dateTimeValue = dateTimeValue;
            _stringValue = stringValue;
        }

        public TreeNode(Dictionary<object, object> attributes, IList<TreeNode> children)
        {
            _attributes = attributes;
            _children = children;
        }

        public Dictionary<object, object> Attributes
        {
            get => _attributes;
            set
            {
                // fake out, read only
            }
        }

        public IList<TreeNode> Children
        {
            get => _children;
            set
            {
                // fake out, read only
            }
        }

        public Guid Id { get; } = Guid.NewGuid();

        public TreeNode Parent { get; set; }

        public static IEnumerable<TreeNode> GetSamples(int sampleCount)
        {
            var samples = new List<TreeNode>(sampleCount);
            for (var currentCount = 0; currentCount < sampleCount; currentCount++)
            {
                samples.Add(GetSample());
            }

            foreach (var sample in samples)
            {
                if (BetterRandom.Odds(0.8))
                {
                    sample.Parent = samples.RandomItem();
                }

                if (BetterRandom.Odds(0.8))
                {
                    foreach (var child in samples.RandomItems(2, 20))
                    {
                        sample.Children.Add(child);
                    }
                }

                var attributesToCreate = BetterRandom.NextInt(0, 5);
                for (var attributeCount = 0; attributeCount < attributesToCreate; attributeCount++)
                {
                    sample.Attributes[samples.RandomItem()] = samples.RandomItem();
                }
            }

            return samples;
        }

        private static TreeNode GetSample()
        {
            var sample = new TreeNode(BetterRandom.NextBool(), BetterRandom.NextChar(), BetterRandom.NextByte(), BetterRandom.NextShort(), BetterRandom.NextInt(), BetterRandom.NextLong(), BetterRandom.NextUShort(), BetterRandom.NextUInt(), BetterRandom.NextULong(), BetterRandom.NextFloat(), BetterRandom.NextDouble(), BetterRandom.NextDateTime(), BetterRandom.NextString(1, 1000, BetterRandom.CharacterGroup.UnicodeWithControlCharacters));
            return sample;
        }

        //public override bool Equals(object obj)
        //{
        //    if (!(obj is TreeNode treeNode2) || treeNode2.GetType() != GetType() || treeNode2.GetHashCode() != GetHashCode())
        //    {
        //        return false;
        //    }

        //    if (!(
        //        _boolValue == treeNode2._boolValue &&
        //        _byteValue == treeNode2._byteValue &&
        //        _charValue == treeNode2._charValue &&
        //        _dateTimeValue == treeNode2._dateTimeValue &&
        //        _doubleValue == treeNode2._doubleValue &&
        //        _floatValue == treeNode2._floatValue &&
        //        _intValue == treeNode2._intValue &&
        //        _longValue == treeNode2._longValue &&
        //        _shortValue == treeNode2._shortValue &&
        //        _stringValue == treeNode2._stringValue &&
        //        _uintValue == treeNode2._uintValue &&
        //        _ulongValue == treeNode2._ulongValue &&
        //        _ushortValue == treeNode2._ushortValue &&
        //        _attributes.Count == treeNode2._attributes.Count &&
        //        _children.Count == treeNode2._children.Count))
        //    {
        //        return false;
        //    }

        //    return true;
        //}

        //public override int GetHashCode() => HashBuilder.Hash(_boolValue, _byteValue, _charValue, _dateTimeValue, _doubleValue, _floatValue, _intValue, _longValue, _shortValue, _stringValue, _uintValue, _ulongValue, _ushortValue);
    }
}
