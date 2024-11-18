using System.Net;
using Core.Feature;

var builder = WebApplication.CreateBuilder(args);

builder.WebHost.ConfigureKestrel(options =>
{
    options.ListenAnyIP(5000);
});

builder.Services.AddFeatures();

var app = builder.Build();

var rootRoute = app.MapGroup("/api/v1");
rootRoute.AppendFeatureRoutes();

app.UseFeatures();

app.Run();
