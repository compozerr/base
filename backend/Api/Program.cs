using Api.Data.Repositories;
using Api.Options;
using Api.Services;
using Core.Extensions;
using Core.Feature;
using Core.MediatR;
using Serilog.Sinks.Humio;

var builder = WebApplication.CreateBuilder(args);

Features.RegisterConfigureCallback<RegisterMediatrServicesFeatureConfigureCallback>();
Features.RegisterConfigureCallback<RegisterValidatorsInAssemblyFeatureConfigureCallback>();
Features.RegisterConfigureCallback<AssembliesFeatureConfigureCallback>();

builder.ConfigureFeatures();
builder.Services.AddFeatures(builder.Configuration);
builder.Services.AddScoped<IEncryptionService, EncryptionService>();
builder.Services.AddScoped<IHashService, HashService>();
builder.Services.AddScoped<IServerRepository, ServerRepository>();
builder.Services.AddScoped<IProjectRepository, ProjectRepository>();
builder.Services.AddScoped<IDeploymentRepository, DeploymentRepository>();
builder.Services.AddScoped<ILocationRepository, LocationRepository>();
builder.Services.AddScoped<IServerService, ServerService>();

builder.Services.AddHttpClient();
builder.Services.AddSingleton<ICryptoService, CryptoService>();
builder.Services.AddTransient<IChildServerHttpClientFactory, ChildServerHttpClientFactory>();

builder.Services.AddRequiredConfigurationOptions<EncryptionOptions>("Encryption");

Log.Logger = new LoggerConfiguration().WriteTo.Console().WriteTo
                                              .HumioSink(new HumioSinkConfiguration
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

app.Run();
