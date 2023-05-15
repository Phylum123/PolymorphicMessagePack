using MessagePack;
using MessagePack.Resolvers;
using ShareAttributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace PolymorphicMessagePack
{

    internal static class GetMarkAttributeClassListExtension
    {
        private static readonly Type _objType = typeof(object);
        public static (Assembly, HashSet<Type>) GetMarkUnionAbsAttributeClasses(this Assembly assembly)
        {
            var markdata = new HashSet<Type>(assembly.GetTypes().Where(x => (x.IsAbstract || x.IsInterface) && x.GetCustomAttribute<UnionAbsOrInterfaceAttribute>(false) != null));
            return (assembly, markdata);
        }

        public static Dictionary<Type, List<Type>> GetAbsDriveClassTypes(this HashSet<Type> types,Assembly target)
        {
            var require_search_types = target.GetTypes().Where(x =>
                //is abstract,is interface,not class,is object,in marked types,is generic are ignore
                !x.IsAbstract && !x.IsInterface && x.IsClass && x != _objType && !types.Contains(x) && !x.IsGenericType
            );

            return AnalysisTypes(require_search_types, types);
        }

        public static Dictionary<Type, List<Type>> GetAbsDriveGenericClassTypes(this HashSet<Type> types, Assembly target)
        {
            var require_search_types = target.GetTypes().Where(x =>
                //is abstract,is interface,not class,is object,in marked types,not generic are ignore
                !x.IsAbstract && !x.IsInterface && x.IsClass && x != _objType && !types.Contains(x) && x.IsGenericType
            );

            return AnalysisTypes(require_search_types, types);
        }

        private static Dictionary<Type, List<Type>> AnalysisTypes(IEnumerable<Type> types, HashSet<Type> exceptTypes)
        {
            Dictionary<Type, List<Type>> cache = new Dictionary<Type, List<Type>>();
            HashSet<Type> visitedScanInterfaceTypes = new HashSet<Type>();

            foreach (var type in types)
            {
                Type temp = type;
                while (temp != null && temp != _objType)
                {
                    var factCheckType = temp.BaseType;
                    //if base type is marked abs
                    //is generic base?
                    if (temp.BaseType.IsGenericType)
                        factCheckType = temp.BaseType.GetGenericTypeDefinition();
                    if (exceptTypes.Contains(factCheckType))
                    {
                        if (!cache.TryGetValue(factCheckType, out var list))
                        {
                            list = new List<Type>() { type };
                            cache.Add(factCheckType, list);
                        }
                        else
                            list.Add(type);
                    }

                    if (!temp.IsAbstract && !visitedScanInterfaceTypes.Contains(temp))
                    {
                        visitedScanInterfaceTypes.Add(temp);
                        foreach (var @interface in temp.GetInterfaces())
                        {
                            //if any interface in marked interface
                            //is generic base?
                            factCheckType = @interface;
                            if (@interface.IsGenericType)
                                factCheckType = @interface.GetGenericTypeDefinition();
                            if (exceptTypes.Contains(factCheckType))
                            {
                                if (!cache.TryGetValue(factCheckType, out var list))
                                {
                                    list = new List<Type>() { type };
                                    cache.Add(factCheckType, list);
                                }
                                else
                                {
                                    list.Add(type);
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
        internal readonly Dictionary<Type, uint> TypeToId = new Dictionary<Type, uint>();
        internal readonly Dictionary<uint, Type> IdToType = new Dictionary<uint, Type>();
        internal readonly HashSet<Type> BaseTypes = new HashSet<Type>();
        internal readonly HashSet<Type> GenericTypes = new HashSet<Type>();
        internal readonly HashSet<Assembly> Assemblies = new HashSet<Assembly>();
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
            TypeToId.Add(typeof(T), typeId);
            IdToType.Add(typeId, typeof(T));
            BaseTypes.Add(typeof(B));
        }

        public void InjectUnionRequireFromAssembly(Assembly assembly,Assembly absClassAssembly)
        {
            if (Assemblies.Contains(assembly) && Assemblies.Contains(absClassAssembly))
                return;
            Assemblies.Add(assembly);
            Assemblies.Add(absClassAssembly);
            //get all mark require union abs/interface
            var markedUnionRequireAbsOrInterfaces = absClassAssembly.GetMarkUnionAbsAttributeClasses();

            //get all drive abs/interface non generic classes
            var allNeedMapNonGenericClasses = markedUnionRequireAbsOrInterfaces.Item2.GetAbsDriveClassTypes(assembly);

            //register them,record relate abs and interface
            foreach (var pair in allNeedMapNonGenericClasses)
            {
                BaseTypes.Add(pair.Key);
                foreach (var pair2 in pair.Value)
                {
                    if (TypeToId.ContainsKey(pair2))
                        continue;
                    var unionIdAttribute = pair2.GetCustomAttribute<RequireUnionAttribute>(false);
                    var pairAttr = pair2.CustomAttributes.First();
                    if (unionIdAttribute == null)
                        throw new ArgumentException(message: $"Shouldn't Happened---{pair2.FullName} not set RequireUnionAttribute but has been scaned");
                    if (IdToType.TryGetValue(unionIdAttribute.UnionUniqueId, out var existMarkType))
                        throw new ArgumentException(message: $"{pair2.FullName} Set union unique Id {unionIdAttribute.UnionUniqueId},but it already been used for {existMarkType.FullName}");

                    TypeToId.Add(pair2, unionIdAttribute.UnionUniqueId);
                    IdToType.Add(unionIdAttribute.UnionUniqueId, pair2);
                }
            }

            //get all drive abs/interface generic classes
            var allNeedRecordGenericClasses = markedUnionRequireAbsOrInterfaces.Item2.GetAbsDriveGenericClassTypes(assembly);
            //record all need prepare generic type class type
            foreach (var pair in allNeedRecordGenericClasses)
            {
                BaseTypes.Add(pair.Key);
                foreach (var pair2 in pair.Value)
                {
                    var unionGenericAttributes = pair2.GetCustomAttributes<RequireUnionGenericAttribute>(false);
                    //truth require register type
                    foreach (var factUsedRuntimeGenericVersion in unionGenericAttributes)
                    {
                        var fType = factUsedRuntimeGenericVersion.SupportGenericType;
                        if (fType.IsGenericType && fType.GetGenericTypeDefinition() == pair2)
                        {
                            if (TypeToId.ContainsKey(fType))
                                continue;
                            if (IdToType.TryGetValue(factUsedRuntimeGenericVersion.UnionUniqueId, out var existMarkType))
                                throw new ArgumentException(message: $"{fType.FullName} Set union unique Id {factUsedRuntimeGenericVersion.UnionUniqueId},but it already been used for {existMarkType.FullName}");
                            TypeToId.Add(fType, factUsedRuntimeGenericVersion.UnionUniqueId);
                            IdToType.Add(factUsedRuntimeGenericVersion.UnionUniqueId, fType);
                        }
                        else
                            throw new ArgumentException(message: $"{fType.FullName} is not genericType or not generic by {pair2.FullName}");
                    }
                }
            }
        }
    }
}
