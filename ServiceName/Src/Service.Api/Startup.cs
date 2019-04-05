using CorrelationId;
using FluentValidation.AspNetCore;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Service.Api.Extensions;

namespace Service.Api
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
            services.AddDefaultLocalization();
            services.AddMvc()
                .SetCompatibilityVersion(CompatibilityVersion.Version_2_2)
                .SetDefaultLocalizationAndJsonSettings()
                .AddMetrics()
                .AddFluentValidation(a => a.RegisterValidatorsFromAssemblyContaining<Startup>());
            services.AddDefaultCors();
            services.AddDefaultHealthChecks();
            services.AddHttpContextAccessor();
            services.AddDefaultSwagger();
            services.AddCorrelationId();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
                app.UseDefaultSwagger();
            }
            else
                app.UseHsts();
            app.UseCorrelationId(new CorrelationIdOptions { UseGuidForCorrelationId = true });
            app.UseDefaultLocalization();
            app.UseDefaultHealthChecks();
            app.UseDefaultCors();
            app.UseHttpsRedirection();
            app.UseDefaultAuthentication();
            app.UseMvc();
        }
    }
}
