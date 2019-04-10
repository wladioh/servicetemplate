using System.Threading.Tasks;
using App.Metrics.AspNetCore;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Service.Infra.ConfigurationService;

namespace Service.Api
{
    public class Program
    {
        public static async Task Main(string[] args) =>
            await CreateWebHostBuilder(args).Build().RunAsync();

        public static IWebHostBuilder CreateWebHostBuilder(string[] args) =>
            WebHost.CreateDefaultBuilder(args)
                .ConfigureKestrel(opt => opt.AddServerHeader = false)
                .ConfigureMetrics()
                .ConfigureConsul()
                .UseMetrics()
                .UseStartup<Startup>();
    }
}
