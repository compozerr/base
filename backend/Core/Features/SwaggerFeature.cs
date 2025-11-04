using Core.Abstractions;
using Core.Feature;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Core.Features;

public class SwaggerFeature : IFeature
{
    void IFeature.ConfigureServices(IServiceCollection services)
    {
        services.AddSwaggerGen(c =>
        {
            c.SwaggerDoc("v1", new() { Title = "compozerr base", Version = "v1" });

            c.SchemaFilter<IdBaseSchemaFilter>();

            // Use fully-qualified names for schema Ids to avoid collisions between same-named DTOs
            c.CustomSchemaIds(type => type.FullName);

            c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme()
            {
                Name = "Authorization",
                Type = SecuritySchemeType.ApiKey,
                Scheme = "Bearer",
                BearerFormat = "JWT",
                In = ParameterLocation.Header,
                Description = "JWT Authorization header using the Bearer scheme. \r\n\r\n Enter 'Bearer' [space] and then your token in the text input below.\r\n\r\nExample: \"Bearer 1safsfsdfdfd\"",
            });
            c.AddSecurityRequirement(new OpenApiSecurityRequirement {
        {
            new OpenApiSecurityScheme {
                Reference = new OpenApiReference {
                    Type = ReferenceType.SecurityScheme,
                        Id = "Bearer"
                }
            },
            new string[] {}
        }
    });
        });
    }

    void IFeature.ConfigureApp(WebApplication app)
    {
        if (app.Environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI();
        }
    }

    public class IdBaseSchemaFilter : ISchemaFilter
    {
        public void Apply(OpenApiSchema schema, SchemaFilterContext context)
        {
            if (IsIId(context.Type))
            {
                schema.Type = "string";
                schema.Format = "uuid";
            }
        }
        private static readonly Type IIdType = typeof(IId<>);
        private static bool IsIId(Type type) =>
            type.GetInterfaces().Any(i => i.IsGenericType && i.GetGenericTypeDefinition() == IIdType);
    }
}