﻿using MessagePack;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PolymorphicMessagePack
{
    public class PolymorphicMessagePackSettings
    {
        internal readonly Dictionary<Type, int> TypeToId = new();
        internal readonly Dictionary<int, Type> IdToType = new();
        internal readonly HashSet<Type> BaseTypes = new();
        internal IFormatterResolver InnerResolver;
        internal Type InnerResolverType;

        public PolymorphicMessagePackSettings(IFormatterResolver innerResolver)
        {
            InnerResolver = innerResolver;
            InnerResolverType = InnerResolver.GetType();
        }

        public bool SerializeOnlyRegisteredTypes { get; set; } = false;

        public void RegisterType<B, T>(int typeId)
            where B : class
            where T : class, B
        {

            if (typeof(T).IsInterface || typeof(T).IsAbstract)
                throw new ArgumentException($"Failed to register derived type '{ typeof(T).FullName }'. It cannot be an interface or an abstract class.", nameof(T));

            if (typeof(T).ContainsGenericParameters)
                throw new ArgumentException($"Failed to register derived type '{ typeof(T).FullName }'. It cannot have open generic parameters. You must replace the open generic parameters with specific types.", nameof(T));

            if (TypeToId.TryGetValue(typeof(T), out var currentId) && currentId != typeId)
                throw new ArgumentException($"Failed to register derived type '{ typeof(T).FullName }'. Type '{ typeof(T).FullName }' is already registered to Type Id: { currentId }", nameof(T));

            if (IdToType.TryGetValue(typeId, out var currentType) && currentType != typeof(T))
                throw new ArgumentException($"Failed to register derived type '{ typeof(T).FullName }'. Type Id: { typeId } is already registered to another type '{ currentType.FullName }'", nameof(typeId));

            //Use TryAdd, becasue the type could already exist and the user is simply trying to add another base class
            TypeToId.TryAdd(typeof(T), typeId);
            IdToType.TryAdd(typeId, typeof(T));
            BaseTypes.Add(typeof(B));
        }


        //TODO: convenience method
        //public void RegisterTypeWithAllInterfacesAndBase<T>(int typeId, bool includeObject = false)
        //    where T : class
        //{
            
        //}

        //TODO: What is the user needs to register 10,000 types? perhaps a way to do entire namspaaces with auto-numbering, if you aren't storing messages?
    }
}
