
using Auth.Models;
using Auth.Repositories;
using Database.Models;
using Database.Repositories;
using Microsoft.AspNetCore.Authorization;
using Template;

namespace Api.Features.Home;

public static class HomeRoute
{
    public static void AddHomeRoute(this IEndpointRouteBuilder app)
    {
        app.MapGet("/", (IConfiguration configuration) => $"{ExampleClass.ExampleMethod()}");
    }
}