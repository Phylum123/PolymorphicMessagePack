using PolymorphicMessagePack.Fody;
using ShareAttributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

#pragma warning disable CS8604

namespace Fody.Test
{
    public class AutoPolyWeaverTests
    {
        static TestResult testResult;

        static AutoPolyWeaverTests()
        {
            var xElement = XElement.Parse("<AutoPolyMsgPackWeaver NameSpace='MsgPackDefineForInject'/>");
            var weavingTask = new AutoPolyMsgPackWeaver { Config= xElement };
            testResult = weavingTask.ExecuteTestRun("MsgPackDefineForInject.dll",runPeVerify:false);
        }

        [Fact]
        public void WillNotInjectNonMsgPackObjAttrClass()
        {
            var type = testResult.Assembly.GetType("MsgPackDefineForInject.Class2");

            var attribute = type.GetCustomAttribute<RequireUnionAttribute>();

            Assert.Null(attribute);
        }

        [Fact]
        public void InjectMsgPackObjAttrClass()
        {
            var type = testResult.Assembly.GetType("MsgPackDefineForInject.Class6");

            var attribute = type.GetCustomAttribute<RequireUnionAttribute>();

            Assert.NotNull(attribute);
        }

        [Fact]
        public void InjectGenericUnionType()
        {
            var type = testResult.Assembly.GetType("MsgPackDefineForInject.Class5`1");

            var attribute = type.GetCustomAttributes<RequireUnionGenericAttribute>();

            Assert.True(attribute.Count() == 2);
        }

        [Fact]
        public void InjectGenericUnionWithSameType()
        {
            var type = testResult.Assembly.GetType("MsgPackDefineForInject.Class7`1");

            var attribute = type.GetCustomAttributes<RequireUnionGenericAttribute>();

            Assert.True(attribute.Count() == 2);
        }
    }
}
