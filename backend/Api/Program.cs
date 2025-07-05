using Core.Extensions;
using Core.Feature;
using Core.Helpers.Env;
using Core.MediatR;

var builder = WebApplication.CreateBuilder(args);

Features.RegisterConfigureCallback<RegisterMediatrServicesFeatureConfigureCallback>();
Features.RegisterConfigureCallback<RegisterValidatorsInAssemblyFeatureConfigureCallback>();
Features.RegisterConfigureCallback<AssembliesFeatureConfigureCallback>();

builder.Configuration.AddEnvFile(".env");

builder.ConfigureFeatures();
builder.Services.AddFeatures(builder.Configuration);
builder.Services.ValidateAllConfiguration();

var app = builder.Build();

app.UseFeatures();

app.Run();
