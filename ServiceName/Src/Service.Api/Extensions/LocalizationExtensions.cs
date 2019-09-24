using System.Globalization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Mvc.Razor;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json.Serialization;
using Service.Api.Resources;

namespace Service.Api.Extensions
{
    public static class LocalizationExtensions
    {
        public static IApplicationBuilder UseDefaultLocalization(this IApplicationBuilder app)
        {
            var supportedCultures = new[]
            {
                new CultureInfo("pt-BR"),
                new CultureInfo("en-US")
            };
            return app.UseRequestLocalization(new RequestLocalizationOptions
            {
                DefaultRequestCulture = new RequestCulture("pt-BR"),
                // Formatting numbers, dates, etc.
                SupportedCultures = supportedCultures,
                // UI strings that we have localized.
                SupportedUICultures = supportedCultures
            });
        }

        public static IMvcBuilder SetDefaultLocalizationAndJsonSettings(this IMvcBuilder builder)
        {
            return builder.AddViewLocalization(LanguageViewLocationExpanderFormat.Suffix)
                .AddDataAnnotationsLocalization(options =>
                {
                    options.DataAnnotationLocalizerProvider = (type, factory) =>
                        factory.Create(typeof(SharedResource));
                }).AddJsonOptions(opt =>
                {
                    //opt.JsonSerializerOptions.PropertyNamingPolicy =  new Camel(); 
                    //SerializerSettings.ContractResolver = new DefaultContractResolver
                    //{
                    //    NamingStrategy = new CamelCaseNamingStrategy()
                    //};
                });
        }

        public static IServiceCollection AddDefaultLocalization(this IServiceCollection services)
        {
            return services.AddLocalization(options => options.ResourcesPath = "Resources");
        }
    }
}
