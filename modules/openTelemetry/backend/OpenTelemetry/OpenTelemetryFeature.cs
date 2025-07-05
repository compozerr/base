using Core.Extensions;
using Core.Feature;
using Core.Helpers;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace OpenTelemetryModule;

public class OpenTelemetryFeature : IFeature
{
    private const string serviceName = "base";
    
    void IFeature.ConfigureServices(IServiceCollection services, IConfiguration configuration)
    {
        services.AddOpenTelemetry()
            .ConfigureResource(resource => resource.AddService(serviceName))
            .WithTracing(tracing => tracing
                .AddAspNetCoreInstrumentation()
                .AddHttpClientInstrumentation()
                .AddEntityFrameworkCoreInstrumentation()
                .AddOtlpExporter(
                    options => options.Endpoint = new Uri(configuration["OtelExporterOtlpEndpoint"].ThrowIfNullOrWhiteSpace("OTEL_EXPORTER_OTLP_ENDPOINT configuration key is required"))
                ))
            .WithMetrics(metrics => metrics
                .AddAspNetCoreInstrumentation()
                .AddHttpClientInstrumentation()
                .AddOtlpExporter(
                    options => options.Endpoint = new Uri(configuration["OtelExporterOtlpEndpoint"].ThrowIfNullOrWhiteSpace("OTEL_EXPORTER_OTLP_ENDPOINT configuration key is required"))
                ));
    }

    void IFeature.ConfigureBuilder(WebApplicationBuilder builder)
    {
        builder.Logging.AddOpenTelemetry(options =>
        {
            options
                .SetResourceBuilder(ResourceBuilder.CreateDefault()
                                                   .AddService(serviceName))
                .AddConsoleExporter();
        });
    }

}