using Core.Feature;
using Serilog.Events;
using Serilog.Sinks.Humio;

namespace Api;

public class ApiFeature : IFeature
{
    void IFeature.ConfigureServices(IServiceCollection services, IConfiguration configuration)
    {
        var isProd = configuration["ASPNETCORE_ENVIRONMENT"] == "Production";
        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Information()
            .MinimumLevel.Override("Microsoft", isProd ? LogEventLevel.Warning : LogEventLevel.Information)
            .Enrich.FromLogContext()
            .WriteTo.Console()
            .WriteTo.HumioSink(new HumioSinkConfiguration
            {
                IngestToken = configuration["HUMIOINGESTTOKEN"],
                Tags = new Dictionary<string, string>
                {
                    {"system", "compozerr"},
                    {"platform", "web"},
                    {"environment", isProd ? "prod" : "dev"}
                },
                Url = "https://cloud.community.humio.com",
            })
            .CreateLogger();
    }
}