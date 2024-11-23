
using Serilog;
using Template;

namespace Api.Features.Home;

public class HomeModule : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        Log.Information("HomeModule Called");
        app.MapGet("/", (IConfiguration configuration) => $"{ExampleClass.ExampleMethod()} config: {configuration["SOMESECRET"]}")
           .WithTags(nameof(Home));
    }
}
