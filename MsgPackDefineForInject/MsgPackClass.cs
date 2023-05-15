using AbsInjectTypeDll;
using MessagePack;
using ShareAttributes;
using System.Runtime.Serialization;

namespace MsgPackDefineForInject
{

    public class Class1 : CBase1
    {

        public long CT1 { get; set; }
    }


    [MessagePackObject]
    public class Class2 : CBase2
    {

        public long CT2 { get; set; }
    }

    [MessagePackObject]
    public class Class2_1 : Class2
    {
        public long CT2_1 { get; set; }
    }
    [MessagePackObject]
    public class Class2_2 : Class2
    {
        public long CT2_2 { get; set; } 
    }

    [RequireUnion(114514)]
    [MessagePackObject]
    public class Class3 : Class1
    {

        public long CT3 { get; set; }
    }

    
    [MessagePackObject]
    public class Class4 : CBase4<string>
    {

        public long CT4 { get; set; }
    }

    [GenericUnion(typeof(int))]
    [GenericUnion(typeof(string))]
    [MessagePackObject]
    public class Class5<T> : CBase6<T>
    {
        public long CT5 { get; set; }
    }

    [MessagePackObject]
    public class Class6 : IBase3
    {
        [Key(0)]
        public long CT6 { get; set; }

        public long CT6_1 { get; set; } = 12306;
        private long CT6_2;
    }


    [GenericUnion(typeof(int))]
    [GenericUnion(typeof(int))]
    [GenericUnion(typeof(string))]
    [GenericUnion(typeof(string))]
    [MessagePackObject]
    public class Class7<T> : CBase6<T>
    {
        public long CT7 { get; set; }
    }


    [RequireUnionGeneric(1,typeof(Class8<int>))]
    [RequireUnionGeneric(2,typeof(Class8<string>))]
    [MessagePackObject]
    public class Class8<T> : CBase6<T>
    {
        public long CT8 { get; set; }
    }

    [MessagePackObject]
    public struct Struct1
    {
        private long ST1 { get; set; }

        [Key(1)]
        public long ST2 { get; set; }

        public long ST3;

        private long ST4;

        internal long ST5;

        public long STT5
        {
            get => ST5;
            set => ST5 = value;
        }

    }
}
