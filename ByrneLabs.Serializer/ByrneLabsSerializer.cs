using System.Diagnostics.CodeAnalysis;

namespace ByrneLabs.Serializer
{
    public static class ByrneLabsSerializer
    {
        public static object Deserialize(byte[] bytes)
        {
            if (bytes == null || bytes.Length == 0)
            {
                return null;
            }

            var deserializer = new Deserializer();

            return deserializer.Deserialize(bytes);
        }

        public static T Deserialize<T>(byte[] bytes) => (T) Deserialize(bytes);

        [SuppressMessage("ReSharper", "ReturnTypeCanBeEnumerable.Global")]
        public static byte[] Serialize(object obj)
        {
            if (obj == null)
            {
                return null;
            }

            var serializer = new Serializer();

            return serializer.Serialize(obj);
        }
    }
}
