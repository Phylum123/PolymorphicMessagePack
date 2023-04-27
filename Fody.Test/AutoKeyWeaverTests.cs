using MessagePack;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
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
    public class AutoKeyWeaverTests
    {
        static TestResult testResult;

        static AutoKeyWeaverTests()
        {
            var xElement = XElement.Parse("<AutoMsgPackKeyWeaver NameSpace='MsgPackDefineForInject'/>");
            var weavingTask = new AutoMsgPackKeyWeaver { Config = xElement };
            testResult = weavingTask.ExecuteTestRun("MsgPackDefineForInject.dll", runPeVerify: false);
        }

        [Fact]
        public void WillNotInjectNonMsgPackObjAttrClass()
        {
            var type = testResult.Assembly.GetType("MsgPackDefineForInject.Class2");

            var attribute = type.GetCustomAttribute<MessagePackObjectAttribute>();

            Assert.NotNull(attribute);
        }
    }
}
