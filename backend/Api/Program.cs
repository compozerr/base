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
builder.Services.ValidateAllConfiguration();
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
