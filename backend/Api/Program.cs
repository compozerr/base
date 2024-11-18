using Core.Feature;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

Log.Logger = new LoggerConfiguration().WriteTo.Console()
                                              .CreateLogger();

builder.ConfigureFeatures();
builder.Services.AddFeatures();

var app = builder.Build();

app.UseFeatures();

app.Run();
