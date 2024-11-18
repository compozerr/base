using System.Net;
using Core.Feature;

var builder = WebApplication.CreateBuilder(args);

builder.ConfigureFeatures();

builder.Services.AddFeatures();

var app = builder.Build();

var rootRoute = app.MapGroup("/api/v1");
rootRoute.AppendFeatureRoutes();

app.UseFeatures();

app.Run();
