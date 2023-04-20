using MessagePack;
using MessagePack.Formatters;
using System;

namespace PolymorphicMessagePack
{
    internal interface IMessagePackDeserializeToObject
    {
        object Deserialize<T>(ref MessagePackReader reader, MessagePackSerializerOptions options);
    }

    public class PolymorphicFormatter<T> : IMessagePackFormatter<T>, IMessagePackDeserializeToObject
    {

        private readonly IMessagePackFormatter<T> _formater;

        public PolymorphicFormatter() { }

        public PolymorphicFormatter(IFormatterResolver resolver)
        {
            _formater = resolver.GetFormatter<T>();
            //target method fact belong to instance
            //var instance = Expression.Constant(formatter);

            ////get method info+it's params
            //var methodInfo = resolver.GetType().GetMethod("Serialize");
            //var methodTypeParams = methodInfo.GetParameters().Select(m => m.ParameterType);
            //var delegateInfo = typeof(SerializeDelegate).GetMethod("Invoke");
            //var delegateTypeParams = delegateInfo.GetParameters().Select(m => m.ParameterType);
            //var delegateArguments = delegateTypeParams.Select(Expression.Parameter).ToArray();

            ////make a transform for delegate to Expr.parameter wrong types
            //var convertedArguments = methodTypeParams.Zip(
            //        delegateTypeParams, delegateArguments,
            //        (methodType, delegateType, delegateArgument) =>
            //        methodType != delegateType
            //        ? (Expression)Expression.Convert(delegateArgument, methodType)
            //        : delegateArgument);
            ////make call
            //MethodCallExpression methodCall = Expression.Call(
            //    instance,
            //    methodInfo,
            //    convertedArguments
            //    );
            ////transform delegate call to instance call-with return type convert
            //Expression convertedMethodCall = delegateInfo.ReturnType == methodInfo.ReturnType
            //                                ? (Expression)methodCall
            //: Expression.Convert(methodCall, delegateInfo.ReturnType);


            ////End
            //_serializeDelegate = Expression.Lambda<SerializeDelegate>(
            //    convertedMethodCall,
            //    delegateArguments
            //    ).Compile();

            ////For Deserialize,Do samething
            //methodInfo = resolver.GetType().GetMethod("Deserialize");
            //methodTypeParams = methodInfo.GetParameters().Select(m => m.ParameterType);
            //delegateInfo = typeof(DeserializeDelegate).GetMethod("Invoke");
            //delegateTypeParams = delegateInfo.GetParameters().Select(m => m.ParameterType);
            //delegateArguments = delegateTypeParams.Select(Expression.Parameter).ToArray();

            //convertedArguments = methodTypeParams.Zip(
            //        delegateTypeParams, delegateArguments,
            //        (methodType, delegateType, delegateArgument) =>
            //        methodType != delegateType
            //        ? (Expression)Expression.Convert(delegateArgument, methodType)
            //        : delegateArgument);
            //methodCall = Expression.Call(
            //    instance,
            //    methodInfo,
            //    convertedArguments
            //    );
            //convertedMethodCall = delegateInfo.ReturnType == methodInfo.ReturnType
            //                                ? (Expression)methodCall
            //: Expression.Convert(methodCall, delegateInfo.ReturnType);

            //_deserializeDelegate = Expression.Lambda<DeserializeDelegate>(
            //    convertedMethodCall,
            //    delegateArguments
            //    ).Compile();
        }

        public void Serialize(ref MessagePackWriter writer, T value, MessagePackSerializerOptions options)
        {

            if (value == null)
            {
                writer.WriteNil();
                return;
            }

            //Could remove this if the settings were part of the regular options 
            if (!(options is PolymorphicMessagePackSerializerOptions polyOptions))
                throw new ArgumentException($"You cannot use a {nameof(PolymorphicResolver)} without also using {nameof(PolymorphicMessagePackSerializerOptions)}", nameof(options));

            var actualtype = value.GetType();

            if (!polyOptions.PolymorphicSettings.TypeToId.TryGetValue(actualtype, out var typeId))
                throw new MessagePackSerializationException($"Type '{actualtype.FullName}' is not registered in {nameof(PolymorphicMessagePackSerializerOptions)}");

            writer.WriteArrayHeader(2);
            writer.WriteUInt32(typeId);

            _formater.Serialize(ref writer, value, options);
        }

        public T Deserialize(ref MessagePackReader reader, MessagePackSerializerOptions options)
        {

            if (reader.TryReadNil())
                return default;


            //Could remove this if the settings were part of the regular options 
            if (!(options is PolymorphicMessagePackSerializerOptions polyOptions))
                throw new ArgumentException($"You cannot use a {nameof(PolymorphicResolver)} without also using {nameof(PolymorphicMessagePackSerializerOptions)}", nameof(options));

            options.Security.DepthStep(ref reader);

            try
            {
                var count = reader.ReadArrayHeader();

                if (count != 2)
                    throw new MessagePackSerializationException("Invalid polymorphic array count");

                var typeId = reader.ReadUInt32();

                if (!polyOptions.PolymorphicSettings.IdToType.TryGetValue(typeId, out var type))
                    throw new MessagePackSerializationException($"Cannot find Type Id: {typeId} registered in {nameof(PolymorphicMessagePackSerializerOptions)}");

                //Bottleneck
                return polyOptions.PolymorphicResolver.InnerDeserialize<T>(type, ref reader, options);
            }
            finally
            {
                reader.Depth--;
            }

        }

        object IMessagePackDeserializeToObject.Deserialize<K>(ref MessagePackReader reader, MessagePackSerializerOptions options)
        {
            var result = _formater.Deserialize(ref reader, options);
            if (result is K fact)
                return fact;
            return default;
        }
    }

}