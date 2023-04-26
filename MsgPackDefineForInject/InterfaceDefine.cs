using ShareAttributes;

namespace MsgPackDefineForInject
{
    [UnionAbsOrInterface]
    public interface Base1
    {

    }

    public interface Base2
    {

    }

    [UnionAbsOrInterface]
    public interface Base3 : Base1
    {

    }

    [UnionAbsOrInterface]
    public interface Base4<T>
    {

    }

    public interface Base5 : Base4<string>
    {

    }
    [UnionAbsOrInterface]
    public interface Base6<T> : Base4<T>
    {

    }
}
