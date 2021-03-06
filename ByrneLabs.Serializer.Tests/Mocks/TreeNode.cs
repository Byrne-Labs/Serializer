﻿using System;
using System.Collections.Generic;
using ByrneLabs.Commons;

namespace ByrneLabs.Serializer.Tests.Mocks
{
    public abstract class TreeNode
    {
        private readonly Dictionary<object, object> _attributes = new Dictionary<object, object>();
        private readonly bool _boolValue;
        private readonly byte _byteValue;
        private readonly char _charValue;
        private readonly DateTime _dateTimeValue;
        private readonly double _doubleValue;
        private readonly float _floatValue;
        private readonly int _intValue;
        private readonly long _longValue;
        private readonly int[][][][] _multiDimensionalArray;
        private readonly short _shortValue;
        private readonly string _stringValue;
        private readonly uint _uintValue;
        private readonly ulong _ulongValue;
        private readonly ushort _ushortValue;

        public TreeNode()
        {
        }

        public TreeNode(bool boolValue, char charValue, byte byteValue, short shortValue, int intValue, long longValue, ushort ushortValue, uint uintValue, ulong ulongValue, float floatValue, double doubleValue, DateTime dateTimeValue, string stringValue, int[][][][] multiDimensionalArray)
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
            _multiDimensionalArray = multiDimensionalArray;
        }

        public Dictionary<object, object> Attributes
        {
            get => _attributes;
            set
            {
                // fake out, read only
            }
        }

        public Guid Id { get; } = Guid.NewGuid();

        public TreeNode Parent { get; set; }

        public string SomeOtherProperty { get; set; }

        public string SomeProperty { get; set; } = "asdf";

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

                if (BetterRandom.Odds(0.8) && sample is TreeBranch treeBranch)
                {
                    foreach (var child in samples.RandomItems(2, 20))
                    {
                        treeBranch.Children.Add(child);
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
            TreeNode sample;
            var multiDimensionalArray = new int[4][][][];
            for (var dim1 = 0; dim1 < 4; dim1++)
            {
                multiDimensionalArray[dim1] = new int[4][][];
                for (var dim2 = 0; dim2 < 4; dim2++)
                {
                    multiDimensionalArray[dim1][dim2] = new int[4][];
                    for (var dim3 = 0; dim3 < 4; dim3++)
                    {
                        multiDimensionalArray[dim1][dim2][dim3] = new int[4];
                        for (var dim4 = 0; dim4 < 4; dim4++)
                        {
                            multiDimensionalArray[dim1][dim2][dim3][dim4] = BetterRandom.Next();
                        }
                    }
                }
            }

            if (BetterRandom.Odds(0.2))
            {
                sample = new TreeLeaf(BetterRandom.NextBool(), BetterRandom.NextChar(), BetterRandom.NextByte(), BetterRandom.NextShort(), BetterRandom.NextInt(), BetterRandom.NextLong(), BetterRandom.NextUShort(), BetterRandom.NextUInt(), BetterRandom.NextULong(), BetterRandom.NextFloat(), BetterRandom.NextDouble(), BetterRandom.NextDateTime(), BetterRandom.NextString(1, 1000), multiDimensionalArray);
            }
            else
            {
                sample = new TreeBranch(BetterRandom.NextBool(), BetterRandom.NextChar(), BetterRandom.NextByte(), BetterRandom.NextShort(), BetterRandom.NextInt(), BetterRandom.NextLong(), BetterRandom.NextUShort(), BetterRandom.NextUInt(), BetterRandom.NextULong(), BetterRandom.NextFloat(), BetterRandom.NextDouble(), BetterRandom.NextDateTime(), BetterRandom.NextString(1, 1000), multiDimensionalArray);
            }

            return sample;
        }

        public override bool Equals(object obj)
        {
            if (!(obj is TreeNode treeNode2) || treeNode2.GetType() != GetType() || treeNode2.GetHashCode() != GetHashCode())
            {
                return false;
            }

            if (!(
                    _boolValue == treeNode2._boolValue &&
                    _byteValue == treeNode2._byteValue &&
                    _charValue == treeNode2._charValue &&
                    _dateTimeValue == treeNode2._dateTimeValue &&
                    _doubleValue == treeNode2._doubleValue &&
                    _floatValue == treeNode2._floatValue &&
                    _intValue == treeNode2._intValue &&
                    _longValue == treeNode2._longValue &&
                    _shortValue == treeNode2._shortValue &&
                    _stringValue == treeNode2._stringValue &&
                    _uintValue == treeNode2._uintValue &&
                    _ulongValue == treeNode2._ulongValue &&
                    _ushortValue == treeNode2._ushortValue &&
                    _attributes.Count == treeNode2._attributes.Count) &&
                ArraysEqual(treeNode2._multiDimensionalArray))
            {
                return false;
            }

            return true;
        }

        public override int GetHashCode() => HashBuilder.Hash(_boolValue, _byteValue, _charValue, _dateTimeValue, _intValue, _longValue, _shortValue, _stringValue, _uintValue, _ulongValue, _ushortValue);

        private bool ArraysEqual(int[][][][] multiDimensionalArray)
        {
            for (var dim1 = 0; dim1 < 4; dim1++)
            {
                for (var dim2 = 0; dim2 < 4; dim2++)
                {
                    for (var dim3 = 0; dim3 < 4; dim3++)
                    {
                        for (var dim4 = 0; dim4 < 4; dim4++)
                        {
                            if (multiDimensionalArray[dim1][dim2][dim3][dim4] != _multiDimensionalArray[dim1][dim2][dim3][dim4])
                            {
                                return false;
                            }
                        }
                    }
                }
            }

            return true;
        }
    }
}
