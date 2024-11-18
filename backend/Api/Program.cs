using Core.Feature;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

Log.Logger = new LoggerConfiguration().WriteTo.Console()
                                              .CreateLogger();

builder.ConfigureFeatures();
builder.Services.AddFeatures();

var app = builder.Build();

var rootRoute = app.MapGroup("/api/v1");
rootRoute.AppendFeatureRoutes();

app.UseFeatures();

app.Run();
