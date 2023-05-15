using AbsInjectTypeDll.DllSubAssembly;
using MagicOnion;
using MagicOnion.Serialization;
using MagicOnion.Server;
using MessagePack;
using MessagePack.Resolvers;
using Microsoft.AspNetCore;
using MsgPackDefineForInject;
using PolymorphicMessagePack;
using Service.Shared;

namespace PolyServer
{
    public class Program
    {
        public static void Main(string[] args)
        {
            CreateWebHostBuilder(args).Build().Run();
        }
        public static IWebHostBuilder CreateWebHostBuilder(string[] args) =>
            WebHost.CreateDefaultBuilder(args)
            .ConfigureLogging(
                (hostingContext, logging) =>
                    {
                        logging.AddConfiguration(hostingContext.Configuration.GetSection("Logging"));
                        logging.AddConsole();
                        logging.AddDebug();
                        logging.AddEventSourceLogger();
                    }
                )
            .UseUrls()
            .UseKestrel()
            .UseStartup<Startup>();
    }
}