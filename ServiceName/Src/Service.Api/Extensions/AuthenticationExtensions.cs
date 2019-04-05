using System;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace Service.Api.Extensions
{
    public static class AuthenticationExtensions
    {
        public static IApplicationBuilder UseDefaultAuthentication(this IApplicationBuilder app)
        {
            return app.UseAuthentication();
        }

        public static IServiceCollection AddDefaultAuthentication(this IServiceCollection services)
        {
            var signingConfigurations = new SigningConfigurations();
            services.AddSingleton(signingConfigurations);
            var tokenConfigurations = new TokenOptions();
            var configuration = services.BuildServiceProvider().GetRequiredService<IConfiguration>();
            new ConfigureFromConfigurationOptions<TokenOptions>(
                    configuration.GetSection(nameof(TokenOptions)))
                    .Configure(tokenConfigurations);
            services.AddSingleton(tokenConfigurations);

            services.AddAuthentication(authOptions =>
            {
                authOptions.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                authOptions.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            }).AddJwtBearer(bearerOptions =>
            {
                var paramsValidation = bearerOptions.TokenValidationParameters;
                paramsValidation.IssuerSigningKey = signingConfigurations.Key;
                paramsValidation.ValidAudience = tokenConfigurations.Audience;
                paramsValidation.ValidIssuer = tokenConfigurations.Issuer;

                // Valida a assinatura de um token recebido
                paramsValidation.ValidateIssuerSigningKey = true;
                paramsValidation.ValidateIssuer = false;
                paramsValidation.ValidateAudience = false;
                // Verifica se um token recebido ainda é válido
                paramsValidation.ValidateLifetime = true;

                // Tempo de tolerância para a expiração de um token (utilizado
                // caso haja problemas de sincronismo de horário entre diferentes
                // computadores envolvidos no processo de comunicação)
                paramsValidation.ClockSkew = TimeSpan.Zero;
            });
            services.AddAuthorization(auth =>
            {
                var policy = new AuthorizationPolicyBuilder()
                       .AddAuthenticationSchemes(JwtBearerDefaults.AuthenticationScheme)
                       .RequireAuthenticatedUser().Build();
                auth.AddPolicy("Bearer", policy);
                auth.DefaultPolicy = policy;
            });
            return services;
        }
        public class SigningConfigurations
        {
            public SymmetricSecurityKey Key { get; set; }
            public SigningCredentials SigningCredentials { get; }

            public SigningConfigurations()
            {
                //todo: get from _appSettings.Secret
                Key = new SymmetricSecurityKey(Encoding.ASCII.GetBytes("#PoSCloUd20178#%"));
                SigningCredentials = new SigningCredentials(Key, SecurityAlgorithms.HmacSha256Signature);
            }
        }

        public class TokenOptions
        {
            public string Audience { get; set; }
            public string Issuer { get; set; }
            public int Seconds { get; set; }
        }
    }
}
