using ShareAttributes;

namespace MsgPackDefineForInject
{
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
