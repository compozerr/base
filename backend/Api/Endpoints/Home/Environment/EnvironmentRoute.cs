using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using MediatR;

namespace Api.Endpoints.Home.Environment;

public static class EnvironmentRoute
{
	public const string Route = "/env"; // Set your route path here

	public static RouteHandlerBuilder AddEnvironmentRoute(this IEndpointRouteBuilder app)
	{
		return app.MapGet(Route, ExecuteAsync);
	}

	public static Task<string> ExecuteAsync(
		IConfiguration configuration)
		=> Task.FromResult(configuration["ASPNETCORE_ENVIRONMENT"] ?? "unknown");
}
