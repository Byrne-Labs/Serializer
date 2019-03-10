using System;
using System.IO;
using System.Runtime.Serialization;

namespace ByrneLabs.Serializer
{
    public class Formatter : IFormatter
    {
        public SerializationBinder Binder
        {
            get => throw new NotSupportedException();
            set => throw new NotSupportedException();
        }

        public StreamingContext Context
        {
            get => throw new NotSupportedException();
            set => throw new NotSupportedException();
        }

        public ISurrogateSelector SurrogateSelector
        {
            get => throw new NotSupportedException();
            set => throw new NotSupportedException();
        }

        public object Deserialize(Stream serializationStream)
        {
            var deserializer = new Deserializer();
            return deserializer.Deserialize(serializationStream);
        }

        public void Serialize(Stream serializationStream, object graph)
        {
            var serializer = new Serializer();
            serializer.Serialize(serializationStream, graph);
        }
    }
}
