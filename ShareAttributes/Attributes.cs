using System;

namespace ShareAttributes
{
    /// <summary>
    /// Abstract/Interface Mark
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Interface, AllowMultiple = false, Inherited = false)]
    public class UnionAbsOrInterfaceAttribute : Attribute
    {

    }

    /// <summary>
    /// GenericClassMark
    /// <para>No need current generic type in param</para>
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
    public class GenericUnionAttribute : Attribute
    {
        public Type GenericUsageType { get; set; }

        public GenericUnionAttribute(Type genericUsageType)
        {
            GenericUsageType = genericUsageType;
        }
    }


    /// <summary>
    /// For non generic class,Mark it's union unique id
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
    public class RequireUnionAttribute : Attribute
    {
        public uint UnionUniqueId { get; private set; }

        public RequireUnionAttribute(uint unionUniqueId)
        {
            UnionUniqueId = unionUniqueId;
        }
    }

    /// <summary>
    /// For generic class,Mark which generic types it will be used 
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
    public class RequireUnionGenericAttribute : RequireUnionAttribute
    {
        public Type SupportGenericType { get; private set; }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="unionUniqueId">unique Id</param>
        /// <param name="supportGeneric">which runtime generic will be used</param>
        public RequireUnionGenericAttribute(uint unionUniqueId, Type supportGeneric)
            : base(unionUniqueId)
        {
            SupportGenericType = supportGeneric;
        }
    }
}
