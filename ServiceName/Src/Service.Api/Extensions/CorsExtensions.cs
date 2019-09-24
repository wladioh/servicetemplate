using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace Service.Api.Extensions
{
    public static class CorsExtensions
    {
        public static void UseDefaultCors(this IApplicationBuilder app)
        {
            app.UseCors("AllowAll");
        }

        public static void AddDefaultCors(this IServiceCollection services)
        {
            services.AddCors(options =>
            {
                options.AddPolicy("AllowAll",
                    builder =>
                    {
                        builder
                            .AllowAnyHeader()
                            .AllowAnyMethod()
                            //.AllowAnyOrigin()
                            .AllowCredentials().Build();
                    });
            });
        }
    }
}
