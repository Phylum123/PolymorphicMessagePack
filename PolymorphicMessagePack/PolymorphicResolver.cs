using MessagePack;
using MessagePack.Formatters;
using System;
using System.Collections.Concurrent;
using System.Linq;

namespace PolymorphicMessagePack
{

    internal sealed class PolymorphicResolver : IFormatterResolver
    {

        private readonly PolymorphicMessagePackSettings _polymorphicSettings;
        private readonly ConcurrentDictionary<Type, IMessagePackDeserializeToObject> _innerDeserializeFormatterCache = new();
        public PolymorphicResolver(PolymorphicMessagePackSettings polymorphicSettings)
        {
            _polymorphicSettings = polymorphicSettings;
        }

        public IMessagePackFormatter<T> GetFormatter<T>()
        {
            //Have to check the type here, because of two reasons:
            //1. Deserialize will not work with non-polymorphic types, as it assumes a typeid as the first item in a two part array, the second being the object itself
            //2. We need the polymorphic settings, and they are an instance and are required to be.

            //If i had the object to be serialized or its actual type, I could make this a lot more efficient and remove the need for the Polymorphic delegate.

            //Can something be optimized here?
            var inType = typeof(T);
            if (_polymorphicSettings.BaseTypes.Contains(inType) || _polymorphicSettings.TypeToId.ContainsKey(inType))
            {
                if (_innerDeserializeFormatterCache.TryGetValue(inType, out var formatter))
                    return (IMessagePackFormatter<T>)formatter;
                else
                {
                    var targetTypeFormatter = new PolymorphicFormatter<T>(_polymorphicSettings.InnerResolver);
                    _innerDeserializeFormatterCache.TryAdd(inType, targetTypeFormatter);
                    return targetTypeFormatter;
                }
            }
            //generic type won't contain in settings,scan generic types
            else if (inType.IsGenericType && _polymorphicSettings.GenericTypes.Contains(inType.GetGenericTypeDefinition()))
            {
                //Nice,this generic with generic param is marked that has union require abs/interface and not registered,register it and create formatter
                var targetTypeFormatter = new PolymorphicFormatter<T>(_polymorphicSettings.InnerResolver);
                _innerDeserializeFormatterCache.TryAdd(inType, targetTypeFormatter);
                //get max id which current used
                var avilableId = _polymorphicSettings.IdToType.Keys.Max() + 1;
                _polymorphicSettings.TypeToId.Add(inType, avilableId);
                _polymorphicSettings.IdToType.Add(avilableId, inType);
                return targetTypeFormatter;
            }
            else if (_polymorphicSettings.SerializeOnlyRegisteredTypes)
                throw new MessagePackSerializationException($"Type '{inType.FullName}' is not registered in the {nameof(PolymorphicMessagePackSettings)} and {nameof(PolymorphicMessagePackSettings.SerializeOnlyRegisteredTypes)} is set to true");

            //Use oher formatter
            return _polymorphicSettings.InnerResolver.GetFormatter<T>();
        }

        internal T InnerDeserialize<T>(Type truthType, ref MessagePackReader reader, MessagePackSerializerOptions options)
        {
            if (_innerDeserializeFormatterCache.TryGetValue(truthType, out var ploymorphicFormatter))
                return (T)ploymorphicFormatter.Deserialize<T>(ref reader, options) ?? default;
            else
            {
                var constructedType = typeof(PolymorphicFormatter<>).MakeGenericType(truthType);
                var instance = (IMessagePackDeserializeToObject)Activator.CreateInstance(constructedType, _polymorphicSettings.InnerResolver);
                _innerDeserializeFormatterCache.TryAdd(truthType, instance);
                return (T)instance.Deserialize<T>(ref reader, options) ?? default;
            }
        }
    }

}