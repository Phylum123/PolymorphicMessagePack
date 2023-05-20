using Grpc.Core;
using MagicOnion.Serialization;
using MessagePack;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace PolymorphicMessagePack
{
    public class MagicOnionPolyMsgPackSerializerProvider : IMagicOnionSerializerProvider
    {
        private class MessagePackMagicOnionSerializer : IMagicOnionSerializer
        {
            private readonly PolymorphicMessagePackSerializerOptions serializerOptions;

            public MessagePackMagicOnionSerializer(PolymorphicMessagePackSerializerOptions serializerOptions)
            {
                this.serializerOptions = serializerOptions;
            }

            public T Deserialize<T>(in ReadOnlySequence<byte> bytes)
            {
                return MessagePackSerializer.Deserialize<T>(in bytes, serializerOptions);
            }

            public void Serialize<T>(IBufferWriter<byte> writer, in T value)
            {
                //we use origin value type instand of fact require type[maybe abstract or interface]
                //ignore valueType
                if (value != null && (typeof(T).IsClass||typeof(T).IsInterface))
                {
                    MessagePackSerializer.Serialize(value.GetType(), writer, value, serializerOptions);
                }
                else
                {
                    MessagePackSerializer.Serialize(writer, value);
                }
                
            }

            void IMagicOnionSerializer.Serialize<T>(IBufferWriter<byte> writer, in T value)
            {
                Serialize(writer, in value);
            }

            T IMagicOnionSerializer.Deserialize<T>(in ReadOnlySequence<byte> bytes)
            {
                return Deserialize<T>(in bytes);
            }
        }

        protected PolymorphicMessagePackSerializerOptions SerializerOptions { get; }

        public MagicOnionPolyMsgPackSerializerProvider(PolymorphicMessagePackSerializerOptions serializerOptions)
        {
            SerializerOptions = serializerOptions;
        }

        public IMagicOnionSerializer Create(MethodType methodType, MethodInfo methodInfo)
        {
            return new MessagePackMagicOnionSerializer(SerializerOptions);
        }
    }
}
