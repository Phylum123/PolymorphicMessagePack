using MessagePack;
using MsgPackDefineForInject;
using PolymorphicMessagePack;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PolyMsgPack.Test
{
    [TestClass]
    public class AutoMsgPackTest
    {
        public AutoMsgPackTest()
        {

        }

        [TestMethod]
        public void AutoMarkKey()
        {
            var s = MessagePackSerializer.Serialize(new Class2_2() { CT2 = 2, CT2_2 = 22, CTB2 = 12 });

            var ds= MessagePackSerializer.Deserialize<Class2_2>(s);
            Assert.IsNotNull(ds);
        }
    }
}
