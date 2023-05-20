using System;
using System.Threading.Tasks;
using AbsInjectTypeDll.DllSubAssembly;
using MagicOnion;
using MsgPackDefineForInject;

namespace Service.Shared
{
    // Defines .NET interface as a Server/Client IDL.
    // The interface is shared between server and client.
    public interface IMyFirstService : IService<IMyFirstService>
    {
        // The return type must be `UnaryResult<T>` or `UnaryResult`.
        UnaryResult<int> SumAsync(int x, int y);
        // `UnaryResult` does not have a return value like `Task`, `ValueTask`, or `void`.
        UnaryResult DoWorkAsync();

        UnaryResult<CBase1> GetTestData(int x);
    }
}
