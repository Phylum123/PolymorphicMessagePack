using ShareAttributes;

namespace MsgPackDefineForInject
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
}
