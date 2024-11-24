using Core.Feature;
using Serilog;
using Serilog.Sinks.Humio;

var builder = WebApplication.CreateBuilder(args);

builder.ConfigureFeatures();
builder.Services.AddFeatures(builder.Configuration);

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
