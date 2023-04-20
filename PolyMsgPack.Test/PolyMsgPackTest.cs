using MessagePack;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using PolymorphicMessagePack;

namespace PolyMsgPack.Test
{
    [TestClass]
    public class PolyMsgPackTest
    {
        PolymorphicMessagePackSerializerOptions _options;
        public PolyMsgPackTest()
        {
            var polySettings = new PolymorphicMessagePackSettings();

            polySettings.InjectUnionRequireFromAssembly(typeof(PolyMsgPackTest).Assembly);

            _options = new PolymorphicMessagePackSerializerOptions(polySettings);
        }

        [TestMethod]
        public void Test_NonGenericToAbs()
        {
            var s = MessagePackSerializer.Serialize(new Class1 { CT1 = 1 }, _options);
            var ds = MessagePackSerializer.Deserialize<CBase1>(s, _options);

            Assert.IsTrue(ds is Class1 ds1 && ds1.CT1 == 1);
        }

        [TestMethod]
        public void Test_NotMarkClassSer()
        {
            var s = MessagePackSerializer.Serialize(new Class2 { CT2 = 1 }, _options);
            Assert.ThrowsException<MessagePackSerializationException>(() => MessagePackSerializer.Deserialize<CBase2>(s, _options));
        }

        [TestMethod]
        public void Test_S_DSToSubOtherSubClass()
        {
            var s = MessagePackSerializer.Serialize(new Class3 { CT3 = 3, CT1 = 1 }, _options);
            var ds = MessagePackSerializer.Deserialize<Class1>(s, _options);
            Assert.IsTrue(ds.CT1 == 1);
        }

        [TestMethod]
        public void Test_S_DSDriveGenericClass()
        {
            var s = MessagePackSerializer.Serialize(new Class4 { CT4 = 4 }, _options);
            var ds = MessagePackSerializer.Deserialize<Class4>(s, _options);
            Assert.IsTrue(ds.CT4 == 4);
        }

        [TestMethod]
        public void Test_S_DSGenericClass()
        {
            var s = MessagePackSerializer.Serialize(new Class5<string> { CT5 = 5 }, _options);
            var ds = MessagePackSerializer.Deserialize<Class5<string>>(s, _options);
            Assert.IsTrue(ds.CT5 == 5);
        }

        [TestMethod]
        public void Test_S_DSWrongGenericClass()
        {
            var s = MessagePackSerializer.Serialize(new Class5<string> { CT5 = 5 }, _options);
            var ds = MessagePackSerializer.Deserialize<Class5<int>>(s, _options);
            Assert.IsNull(ds);
        }

        [TestMethod]
        public void Test_S_DS_MultiInterface()
        {
            var s = MessagePackSerializer.Serialize(new Class6(), _options);
            var ds1 = MessagePackSerializer.Deserialize<Base3>(s, _options);
            var ds2 = MessagePackSerializer.Deserialize<Base1>(s, _options);
            Assert.IsNotNull(ds1);
            Assert.IsNotNull(ds2);
        }
    }
}
