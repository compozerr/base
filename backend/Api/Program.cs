using Core.Feature;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddFeatures();

var app = builder.Build();

app.UseFeatures();

app.Run();
