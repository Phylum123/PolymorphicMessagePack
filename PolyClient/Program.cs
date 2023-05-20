

using AbsInjectTypeDll.DllSubAssembly;
using Grpc.Net.Client;
using MagicOnion.Client;
using MessagePack.Resolvers;
using MessagePack;
using MsgPackDefineForInject;
using Service.Shared;
using PolymorphicMessagePack;

namespace PolyClient
{
    internal class Program
    {
        static async Task Main(string[] args)
        {
            RegisterResolvers();
            // Connect to the server using gRPC channel.
            var channel = GrpcChannel.ForAddress("https://localhost:5001");

            // NOTE: If your project targets non-.NET Standard 2.1, use `Grpc.Core.Channel` class instead.
            // var channel = new Channel("localhost", 5001, new SslCredentials());

            // Create a proxy to call the server transparently.
            var client = MagicOnionClient.Create<IMyFirstService>(channel);

            // Call the server-side method using the proxy.
            var resultClass = await client.GetTestData(3);
            Console.WriteLine($"Result: {resultClass.GetType().Name}");
        }

        static void RegisterResolvers()
        {
            var polySettings = new PolymorphicMessagePackSettings(StandardResolver.Instance);

            polySettings.InjectUnionRequireFromAssembly(typeof(Class1).Assembly, typeof(CBase1).Assembly);

            var _polyOptions = new PolymorphicMessagePackSerializerOptions(polySettings);

            MessagePackSerializer.DefaultOptions = _polyOptions;
        }
    }
}