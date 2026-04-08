// Backend API entrypoint
using Api.Endpoints.Organizations;
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

// Register org routes that need Api.Data references (can't live in Organizations module due to circular dep)
var orgApiGroup = app.MapGroup("v1/organizations")
   .RequireAuthorization()
   .RequireRateLimiting("authenticated")
   .WithTags("Organizations");

orgApiGroup.AddTransferProjectRoute();
orgApiGroup.AddSearchGithubUsersRoute();

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