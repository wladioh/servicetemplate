﻿using System;
using System.Collections.Generic;
using HealthChecks.UI.Client;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Service.Api.Extensions
{
    public static class StartupExtensions
    {
        public static void UseDefaultHealthChecks(this IApplicationBuilder app)
        {
            app.UseHealthChecks("/hc", new HealthCheckOptions
            {
                Predicate = _ => true,
                ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
            }).UseHealthChecks("/liveness", new HealthCheckOptions
            {
                Predicate = r => r.Name.Contains("self")
            }).UseHealthChecks("/dependecies", new HealthCheckOptions
            {
                Predicate = r => r.Name.Contains("dependecies"),
                ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
            });
        }

        public static void AddDefaultHealthChecks(this IServiceCollection services)
        {
            services.AddHealthChecks()
                .AddCheck("self", () => HealthCheckResult.Healthy())
                .AddUrlGroup( new List<Uri>{
                    new Uri("https://www.google.com.br")
                }, "dependecies");
        }
    }
}
