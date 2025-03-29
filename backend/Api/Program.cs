using Core.Feature;
using Core.MediatR;
using Serilog.Events;
using Serilog.Sinks.Humio;

var builder = WebApplication.CreateBuilder(args);

Features.RegisterConfigureCallback<RegisterMediatrServicesFeatureConfigureCallback>();
Features.RegisterConfigureCallback<RegisterValidatorsInAssemblyFeatureConfigureCallback>();
Features.RegisterConfigureCallback<AssembliesFeatureConfigureCallback>();

builder.ConfigureFeatures();
builder.Services.AddFeatures(builder.Configuration);

// Configure Serilog
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .WriteTo.HumioSink(new HumioSinkConfiguration
    {
        IngestToken = builder.Configuration["HUMIOINGESTTOKEN"],
        Tags = new Dictionary<string, string>
        {
            {"system", "compozerr"},
            {"platform", "web"},
            {"environment", builder.Environment.EnvironmentName}
        },
        Url = "https://cloud.community.humio.com",
    })
    .CreateLogger();

var app = builder.Build();

app.UseFeatures();

Log.Information("Application starting up");

try
{
    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}