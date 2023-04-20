using MessagePack;
using MessagePack.Resolvers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace PolymorphicMessagePack
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Interface, AllowMultiple = false, Inherited = false)]
    public class UnionAbsOrInterfaceAttribute : Attribute
    {

    }

    public static class GetMarkAttributeClassListExtension
    {
        private static readonly Type _objType = typeof(object);
        public static (Assembly, HashSet<Type>) GetMarkUnionAbsAttributeClasses(this Assembly assembly)
        {
            return (assembly, assembly.GetTypes().Where(x => (x.IsAbstract || x.IsInterface) && x.GetCustomAttribute<UnionAbsOrInterfaceAttribute>(false) != null).ToHashSet());
        }

        public static Dictionary<Type, List<Type>> GetAbsDriveClassTypes(this (Assembly assembly, HashSet<Type> types) data)
        {
            var require_search_types = data.assembly.GetTypes().Where(x =>
                //is abstract,is interface,not class,is object,in marked types,is generic are ignore
                !x.IsAbstract && !x.IsInterface && x.IsClass && x != _objType && !data.types.Contains(x) && !x.IsGenericType
            );

            Dictionary<Type, List<Type>> cache = new();
            HashSet<Type> visitedScanInterfaceTypes = new();

            foreach (var type in require_search_types)
            {
                Type temp = type;
                while (temp != null && temp != _objType)
                {
                    //if base type is marked abs
                    //is generic base?
                    if (temp.BaseType.IsGenericType)
                    {
                        var clothType = temp.BaseType.GetGenericTypeDefinition();
                        if (data.types.Contains(clothType))
                        {
                            if (!cache.TryGetValue(clothType, out var list))
                            {
                                list = new List<Type>() { type };
                                cache.Add(clothType, list);
                            }
                            else
                            {
                                list.Add(type);
                            }
                        }
                    }
                    else
                    {
                        if (data.types.Contains(temp.BaseType))
                        {
                            if (!cache.TryGetValue(temp.BaseType, out var list))
                            {
                                list = new List<Type>() { type };
                                cache.Add(temp.BaseType, list);
                            }
                            else
                            {
                                list.Add(type);
                            }
                        }
                    }
                    if (!temp.IsAbstract && !visitedScanInterfaceTypes.Contains(temp))
                    {
                        visitedScanInterfaceTypes.Add(temp);
                        foreach (var @interface in temp.GetInterfaces())
                        {
                            //if any interface in marked interface
                            //is generic base?
                            if (@interface.IsGenericType)
                            {
                                var clothType = @interface.GetGenericTypeDefinition();
                                if (data.types.Contains(clothType))
                                {
                                    if (!cache.TryGetValue(clothType, out var list))
                                    {
                                        list = new List<Type>() { type };
                                        cache.Add(clothType, list);
                                    }
                                    else
                                    {
                                        list.Add(type);
                                    }
                                }
                            }
                            else
                            {

                                if (data.types.Contains(@interface))
                                {
                                    if (!cache.TryGetValue(@interface, out var list))
                                    {
                                        list = new List<Type>() { type };
                                        cache.Add(@interface, list);
                                    }
                                    else
                                    {
                                        list.Add(type);
                                    }
                                }
                            }
                        }
                    }
                    temp = temp.BaseType;
                }
            }
            return cache;
        }

        public static Dictionary<Type, List<Type>> GetAbsDriveGenericClassTypes(this (Assembly assembly, HashSet<Type> types) data)
        {
            var require_search_types = data.assembly.GetTypes().Where(x =>
                //is abstract,is interface,not class,is object,in marked types,not generic are ignore
                !x.IsAbstract && !x.IsInterface && x.IsClass && x != _objType && !data.types.Contains(x) && x.IsGenericType
            );

            Dictionary<Type, List<Type>> cache = new();
            HashSet<Type> visitedScanInterfaceTypes = new();

            foreach (var type in require_search_types)
            {
                Type temp = type;
                while (temp != null && temp != _objType)
                {
                    //if base type is marked abs
                    //is generic base?
                    if (temp.BaseType.IsGenericType)
                    {
                        var clothType = temp.BaseType.GetGenericTypeDefinition();
                        if (data.types.Contains(clothType))
                        {
                            if (!cache.TryGetValue(temp.BaseType, out var list))
                            {
                                list = new List<Type>() { type };
                                cache.Add(temp.BaseType, list);
                            }
                            else
                            {
                                list.Add(type);
                            }
                        }
                    }
                    else
                    {
                        if (data.types.Contains(temp.BaseType))
                        {
                            if (!cache.TryGetValue(temp.BaseType, out var list))
                            {
                                list = new List<Type>() { type };
                                cache.Add(temp.BaseType, list);
                            }
                            else
                            {
                                list.Add(type);
                            }
                        }
                    }



                    if (!temp.IsAbstract && !visitedScanInterfaceTypes.Contains(temp))
                    {
                        visitedScanInterfaceTypes.Add(temp);
                        foreach (var @interface in temp.GetInterfaces())
                        {
                            //if any interface in marked interface
                            //is generic base?
                            if (@interface.IsGenericType)
                            {
                                var clothType = @interface.GetGenericTypeDefinition();
                                if (data.types.Contains(clothType))
                                {
                                    if (!cache.TryGetValue(@interface, out var list))
                                    {
                                        list = new List<Type>() { type };
                                        cache.Add(@interface, list);
                                    }
                                    else
                                    {
                                        list.Add(type);
                                    }
                                }
                            }
                            else
                            {

                                if (data.types.Contains(@interface))
                                {
                                    if (!cache.TryGetValue(@interface, out var list))
                                    {
                                        list = new List<Type>() { type };
                                        cache.Add(@interface, list);
                                    }
                                    else
                                    {
                                        list.Add(type);
                                    }
                                }
                            }
                        }
                    }
                    temp = temp.BaseType;
                }
            }

            return cache;
        }
    }

    public class PolymorphicMessagePackSettings
    {
        internal readonly Dictionary<Type, uint> TypeToId = new();
        internal readonly Dictionary<uint, Type> IdToType = new();
        internal readonly HashSet<Type> BaseTypes = new();
        internal readonly HashSet<Type> GenericTypes = new();
        internal readonly HashSet<Assembly> Assemblies = new();
        internal IFormatterResolver InnerResolver;
        internal Type InnerResolverType;

        /// <summary>
        /// Use MsgPack StandardResolver
        /// </summary>
        public PolymorphicMessagePackSettings()
        {
            InnerResolver = StandardResolver.Instance;
            InnerResolverType = InnerResolver.GetType();
        }
        /// <summary>
        /// Use your own resolver
        /// </summary>
        /// <param name="innerResolver"></param>
        public PolymorphicMessagePackSettings(IFormatterResolver innerResolver)
        {
            InnerResolver = innerResolver;
            InnerResolverType = InnerResolver.GetType();
        }

        public bool SerializeOnlyRegisteredTypes { get; set; } = false;

        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="B">abstract class or interface</typeparam>
        /// <typeparam name="T">target class</typeparam>
        /// <param name="typeId"></param>
        /// <exception cref="ArgumentException"></exception>
        public void RegisterType<B, T>(uint typeId)
            where B : class
            where T : class, B
        {

            if (typeof(T).IsInterface || typeof(T).IsAbstract)
                throw new ArgumentException($"Failed to register derived type '{typeof(T).FullName}'. It cannot be an interface or an abstract class.", nameof(T));

            if (typeof(T).ContainsGenericParameters)
                throw new ArgumentException($"Failed to register derived type '{typeof(T).FullName}'. It cannot have open generic parameters. You must replace the open generic parameters with specific types.", nameof(T));

            if (TypeToId.TryGetValue(typeof(T), out var currentId) && currentId != typeId)
                throw new ArgumentException($"Failed to register derived type '{typeof(T).FullName}'. Type '{typeof(T).FullName}' is already registered to Type Id: {currentId}", nameof(T));

            if (IdToType.TryGetValue(typeId, out var currentType) && currentType != typeof(T))
                throw new ArgumentException($"Failed to register derived type '{typeof(T).FullName}'. Type Id: {typeId} is already registered to another type '{currentType.FullName}'", nameof(typeId));

            //Use TryAdd, becasue the type could already exist and the user is simply trying to add another base class
            TypeToId.TryAdd(typeof(T), typeId);
            IdToType.TryAdd(typeId, typeof(T));
            BaseTypes.Add(typeof(B));
        }

        public void InjectUnionRequireFromAssembly(Assembly assembly)
        {
            if (Assemblies.Contains(assembly))
                return;
            Assemblies.Add(assembly);
            //prepare auto increase key id
            uint startId = 0;
            if (IdToType.Count > 0)
                startId = IdToType.Keys.Max() + 1;

            //get all mark require union abs/interface
            var markedUnionRequireAbsOrInterfaces = assembly.GetMarkUnionAbsAttributeClasses();

            //get all drive abs/interface non generic classes
            var allNeedMapNonGenericClasses = markedUnionRequireAbsOrInterfaces.GetAbsDriveClassTypes();

            //register them,record relate abs and interface
            foreach (var pair in allNeedMapNonGenericClasses)
            {
                BaseTypes.Add(pair.Key);
                foreach (var pair2 in pair.Value)
                {
                    if (TypeToId.ContainsKey(pair2))
                        continue;
                    TypeToId.TryAdd(pair2, startId);
                    IdToType.TryAdd(startId, pair2);
                    startId++;
                }
            }

            //get all drive abs/interface generic classes
            var allNeedRecordGenericClasses = markedUnionRequireAbsOrInterfaces.GetAbsDriveGenericClassTypes();
            //record all need prepare generic type class type
            //can't scan from static code,only can know what actually type used for them
            //e.g: abstract class A {}   class B<T>:A{}
            //this can be scan and find define for B<T>->B<>
            //but can't know what really T will be used which type 
            foreach (var pair in allNeedRecordGenericClasses)
            {
                BaseTypes.Add(pair.Key);
                foreach (var pair2 in pair.Value)
                {
                    if (GenericTypes.Contains(pair2))
                        continue;
                    GenericTypes.Add(pair2);
                }
            }


        }
    }
}
