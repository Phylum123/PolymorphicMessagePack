using ShareAttributes;
using System;

namespace AbsInjectTypeDll.DllSubAssembly
{

    [UnionAbsOrInterface]
    public abstract class CBase1
    {
    }

    public abstract class CBase2
    {
        public long CTB2 { get; set; }
    }

    [UnionAbsOrInterface]
    public abstract class CBase3 : CBase1
    {

    }

    [UnionAbsOrInterface]
    public abstract class CBase4<T>
    {

    }

    public abstract class CBase5 : CBase4<string>
    {

    }
    [UnionAbsOrInterface]
    public abstract class CBase6<T> : CBase4<T>
    {

    }

    [UnionAbsOrInterface]
    public interface IBase1
    {

    }

    public interface IBase2
    {

    }

    [UnionAbsOrInterface]
    public interface IBase3 : IBase1
    {

    }

    [UnionAbsOrInterface]
    public interface IBase4<T>
    {

    }

    public interface IBase5 : IBase4<string>
    {

    }
    [UnionAbsOrInterface]
    public interface IBase6<T> : IBase4<T>
    {

    }
}
