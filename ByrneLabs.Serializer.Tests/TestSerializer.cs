using System.Collections.Generic;
using System.Linq;
using ByrneLabs.Serializer.Tests.Mocks;
using Xunit;

namespace ByrneLabs.Serializer.Tests
{
    public class TestSerializer
    {
        [Fact]
        public void TestDeserialize()
        {
            var samples = TreeNode.GetSamples(1000).ToArray();

            var serializedBytes = ByrneLabsSerializer.Serialize(samples);

            var deserializedSamples = ByrneLabsSerializer.Deserialize<IEnumerable<TreeNode>>(serializedBytes).ToArray();

            Assert.Equal(samples.Length, deserializedSamples.Length);

            for (var sampleIndex = 0; sampleIndex < samples.Length; sampleIndex++)
            {
                Assert.Equal(samples[sampleIndex], deserializedSamples[sampleIndex]);
            }
        }

        [Fact]
        public void TestSerialize()
        {
            var samples = TreeNode.GetSamples(1000);

            var serializedBytes = ByrneLabsSerializer.Serialize(samples);

            Assert.NotEmpty(serializedBytes);
        }
    }
}
