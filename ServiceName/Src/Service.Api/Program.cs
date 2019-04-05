using System.Threading.Tasks;
using App.Metrics.AspNetCore;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;

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
                .UseMetrics()
                .UseStartup<Startup>();
    }
}
