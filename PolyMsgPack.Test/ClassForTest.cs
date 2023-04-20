using MessagePack;

namespace PolyMsgPack.Test
{
    [MessagePackObject]
    public class Class1 : CBase1
    {
        [Key(0)]
        public long CT1 { get; set; }
    }
    [MessagePackObject]
    public class Class2 : CBase2
    {
        [Key(0)]
        public long CT2 { get; set; }
    }
    [MessagePackObject]
    public class Class3 : Class1
    {
        [Key(1)]
        public long CT3 { get; set; }
    }
    [MessagePackObject]
    public class Class4 : CBase4<string>
    {
        [Key(0)]
        public long CT4 { get; set; }
    }
    [MessagePackObject]
    public class Class5<T> : CBase6<T>
    {
        [Key(0)]
        public long CT5 { get; set; }
    }

    [MessagePackObject]
    public class Class6 : Base3
    {

    }

}
