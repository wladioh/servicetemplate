using CorrelationId;
using FluentValidation.AspNetCore;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Rebus.Routing.TypeBased;
using Service.Api.Extensions;
using Service.Infra.ConfigurationService;
using Service.Infra.Database.Mongo;
using Service.Infra.MessageBus.Rebus;
using Service.Infra.Network;
using Service.Infra.Repositories;

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
            services.AddOptions();
            services.Configure<EndpointsOptions>(Configuration.GetSection(EndpointsOptions.Section));
            services.AddTransient(resolver => resolver.GetService<IOptionsMonitor<EndpointsOptions>>().CurrentValue);            
            services.AddDistributedMemoryCache();
            services.AddResponseCaching();
            services.AddResponseCompression();
            services.AddRefitWithPolly<EndpointsOptions>(Configuration, config =>
                {
                    config.Configure<ISomeoneApi>(it => it.Mock);
                });
            services.AddMongo();
            services.AddMongoRepositories();
            services.AddRebus<Startup>(Configuration, configurer =>
            {
                configurer.TypeBased()
                    .Map<OtherMessage>("OtherService")
                    .MapFallback("ServiceNameErros");
            });
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
            app.UseRefitWithPolly();
            app.UseDefaultLocalization();
            app.UseDefaultHealthChecks();
            app.UseDefaultCors();
            app.UseHttpsRedirection();
            app.UseDefaultAuthentication();
            app.UseConsul();
            app.UseResponseCaching();
            app.UseResponseCompression();
            app.UseMvc();
            app.UseRebus();
        }
    }
}
