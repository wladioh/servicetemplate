using System.Threading.Tasks;
using App.Metrics.AspNetCore;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using Service.Infra.ConfigurationService;

namespace Service.Api
{
    public class Program
    {
        public static async Task Main(string[] args) =>
            await CreateWebHostBuilder(args).Build().RunAsync();

        public static IHostBuilder CreateWebHostBuilder(params string[] args) =>
           Host.CreateDefaultBuilder(args)
            .ConfigureConsul()
            .ConfigureWebHost(builder =>
           {
               builder.
               UseKestrel(options => options.AddServerHeader = false)
                .ConfigureMetrics()
               // .UseMetrics()
                .UseStartup<Startup>();
           });
    }
}
