using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Service.Contract.Tests.Middleware;

namespace Service.Contract.Tests
{
    public class PactStartup
    {
        public PactStartup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddSingleton<ProviderStateService>();
            services.AddMvcCore();
            services.AddControllers();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app)
        {
            app.UseRouting();
            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });
        }
    }

    [Route("/[controller]")]
    [ApiController]
    public class ProviderController : ControllerBase
    {
        private readonly ProviderStateService providerStateService;

        public ProviderController(ProviderStateService providerStateService)
        {
            this.providerStateService = providerStateService;
        }

        [HttpGet("/states")]
        public async Task<IActionResult> Get(ProviderState providerState)
        {
            await providerStateService.Provide(providerState);
            return Ok();
        }
    }
}
