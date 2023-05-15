using AbsInjectTypeDll.DllSubAssembly;
using MessagePack;
using MessagePack.Resolvers;
using MsgPackDefineForInject;
using PolymorphicMessagePack;

namespace PolyMsgPack.Test
{

    [TestClass]
    public class PolyMsgPackTest
    {
        PolymorphicMessagePackSerializerOptions _polyOptions;
        public PolyMsgPackTest()
        {
            var polySettings = new PolymorphicMessagePackSettings(StandardResolver.Instance);

            polySettings.InjectUnionRequireFromAssembly(typeof(Class1).Assembly,typeof(CBase1).Assembly);

            _polyOptions = new PolymorphicMessagePackSerializerOptions(polySettings);

            MessagePackSerializer.DefaultOptions = _polyOptions;
        }

        [TestMethod]
        public void Test_NonGenericToAbs()
        {
            var s = MessagePackSerializer.Serialize(new Class3 { CT1 = 3,CT3=4});
            var ds = MessagePackSerializer.Deserialize<CBase1>(s);

            Assert.IsTrue(ds is Class1 ds1 && ds1.CT1 == 3);
        }

        [TestMethod]
        public void Test_NotMarkClassSer()
        {
            var s = MessagePackSerializer.Serialize(new Class2 { CT2 = 1 }, _polyOptions);
            Assert.ThrowsException<MessagePackSerializationException>(() => MessagePackSerializer.Deserialize<CBase2>(s, _polyOptions));
        }

        [TestMethod]
        public void Test_S_DSToSubOtherSubClass()
        {
            var s = MessagePackSerializer.Serialize(new Class3 { CT3 = 3, CT1 = 1 }, _polyOptions);
            var ds = MessagePackSerializer.Deserialize<Class1>(s, _polyOptions);
            Assert.IsTrue(ds.CT1 == 1);
        }

        [TestMethod]
        public void Test_S_DSDriveGenericClass()
        {
            var s = MessagePackSerializer.Serialize(new Class4 { CT4 = 4 }, _polyOptions);
            var ds = MessagePackSerializer.Deserialize<Class4>(s, _polyOptions);
            Assert.IsTrue(ds.CT4 == 4);
        }

        [TestMethod]
        public void Test_S_DSGenericClass()
        {
            var s = MessagePackSerializer.Serialize(new Class5<string> { CT5 = 5 }, _polyOptions);
            var ds = MessagePackSerializer.Deserialize<Class5<string>>(s, _polyOptions);
            Assert.IsTrue(ds.CT5 == 5);
        }

        [TestMethod]
        public void Test_S_DSWrongGenericClass()
        {
            var s = MessagePackSerializer.Serialize(new Class5<string> { CT5 = 5 }, _polyOptions);
            var ds = MessagePackSerializer.Deserialize<Class5<int>>(s, _polyOptions);
            Assert.IsNull(ds);
        }

        [TestMethod]
        public void Test_S_DS_MultiInterface()
        {
            var s = MessagePackSerializer.Serialize(new Class6(), _polyOptions);
            var ds1 = MessagePackSerializer.Deserialize<IBase3>(s, _polyOptions);
            var ds2 = MessagePackSerializer.Deserialize<IBase1>(s, _polyOptions);
            Assert.IsNotNull(ds1);
            Assert.IsNotNull(ds2);
        }
    }
}
