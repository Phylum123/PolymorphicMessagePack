using MagicOnion.Server;
using MagicOnion;
using Service.Shared;
using MsgPackDefineForInject;
using AbsInjectTypeDll.DllSubAssembly;

namespace PolyServer
{
    // Implements RPC service in the server project.
    // The implementation class must inherit `ServiceBase<IMyFirstService>` and `IMyFirstService`
    public class MyFirstService : ServiceBase<IMyFirstService>, IMyFirstService
    {
        // `UnaryResult<T>` allows the method to be treated as `async` method.
        public async UnaryResult<int> SumAsync(int x, int y)
        {
            Console.WriteLine($"Received:{x}, {y}");
            return x + y;
        }

        public async UnaryResult DoWorkAsync()
        {
            // Something to do ...
        }

        public async UnaryResult<CBase1> GetTestData(int x)
        {
            Console.WriteLine($"Received:{x}");
            var package=new Class3() { CT1 = x, CT3 = 3 };
            return package;
        }
    }
}
