using System.Collections.Generic;
using System.Linq;
using System.Net;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Swashbuckle.AspNetCore.Swagger;
using Swashbuckle.AspNetCore.SwaggerGen;
using Swashbuckle.AspNetCore.SwaggerUI;

namespace Service.Api.Extensions
{
    public static class SwaggerExtensions
    {
        public static void UseDefaultSwagger(this IApplicationBuilder app)
        {
            app.UseSwagger();
            app.UseSwaggerUI(c =>
            {
                c.DefaultModelsExpandDepth(-1);
                c.DefaultModelRendering(ModelRendering.Example);
                c.DocExpansion(DocExpansion.None);
                c.SwaggerEndpoint("/swagger/v1/swagger.json", "PosState Service V1");
            });
        }

        public static void AddDefaultSwagger(this IServiceCollection services)
        {
            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new Info { Title = "PosState Service", Version = "v1" });
                c.AddSecurityDefinition("Bearer", new ApiKeyScheme
                {
                    In = "header",
                    Description = "Please insert JWT with Bearer into field",
                    Name = "Authorization",
                    Type = "apiKey"
                });
                c.AddSecurityRequirement(new Dictionary<string, IEnumerable<string>>
                {
                    { "Bearer", new string[] { } }
                });
                c.DocumentFilter<SecurityRequirementsDocumentFilter>();
                c.OperationFilter<AuthResponsesOperationFilter>();
                c.OperationFilter<CultureFilter>();
                c.CustomSchemaIds(x => x.FullName);
            });
        }

        public class SecurityRequirementsDocumentFilter : IDocumentFilter
        {
            public void Apply(SwaggerDocument document, DocumentFilterContext context)
            {
                document.Security = new List<IDictionary<string, IEnumerable<string>>>
                {
                    new Dictionary<string, IEnumerable<string>>
                    {
                        { "Bearer", new string[]{ } },
                        { "Basic", new string[]{ } },
                    }
                };
            }
        }

        public class AuthResponsesOperationFilter : IOperationFilter
        {
            public void Apply(Operation operation, OperationFilterContext context)
            {
                var authAttributes = context.MethodInfo.DeclaringType.GetCustomAttributes(true)
                    .Union(context.MethodInfo.GetCustomAttributes(true))
                    .OfType<AuthorizeAttribute>();

                if (authAttributes.Any())
                    operation.Responses.Add(HttpStatusCode.Unauthorized.ToString(), new Response { Description = "Unauthorized" });
            }
        }

        public class CultureFilter : IOperationFilter
        {
            public void Apply(Operation operation, OperationFilterContext context)
            {
                if (operation.Parameters == null)
                    operation.Parameters = new List<IParameter>();

                operation.Parameters.Add(new NonBodyParameter
                {
                    Name = "Accept-Language",
                    In = "header",
                    Type = "string",
                    Required = false
                });
            }
        }
    }
}
