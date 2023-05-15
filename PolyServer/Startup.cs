using AbsInjectTypeDll.DllSubAssembly;
using Grpc.Net.Client;
using MagicOnion.Server;
using MessagePack.Resolvers;
using MsgPackDefineForInject;
using PolymorphicMessagePack;

namespace PolyServer
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddControllersWithViews();

            services.AddGrpc(); // MagicOnion depends on ASP.NET Core gRPC service.

            var polySettings = new PolymorphicMessagePackSettings(StandardResolver.Instance);
            polySettings.InjectUnionRequireFromAssembly(typeof(Class1).Assembly, typeof(CBase1).Assembly);
            var _polyOptions = new PolymorphicMessagePackSerializerOptions(polySettings);

            services.AddMagicOnion(options =>
            {
                options.MessageSerializer = new MagicOnionPolyMsgPackSerializerProvider(_polyOptions);
            });
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseHttpsRedirection();

            app.UseRouting();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapMagicOnionHttpGateway("_",
                    app.ApplicationServices.GetService<MagicOnionServiceDefinition>()!.MethodHandlers,
                    GrpcChannel.ForAddress($"https://localhost:{Configuration["GrpcChannelPort"]}"));
                endpoints.MapMagicOnionSwagger("mo/swagger",
                    app.ApplicationServices.GetService<MagicOnionServiceDefinition>()!.MethodHandlers,
                    "/_/");
                endpoints.MapMagicOnionService();
            });
        }
    }
}
