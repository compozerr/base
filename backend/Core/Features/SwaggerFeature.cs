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