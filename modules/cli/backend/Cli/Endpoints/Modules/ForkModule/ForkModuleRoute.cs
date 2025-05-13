using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using MediatR;

namespace Cli.Endpoints.Modules.ForkModule;

public static class ForkModuleRoute
{
	public const string Route = "fork-modules"; // Set your route path here

	public static RouteHandlerBuilder AddForkModuleRoute(this IEndpointRouteBuilder app)
	{
		return app.MapPost(Route, ExecuteAsync);
	}

	public static Task<ForkModuleResponse> ExecuteAsync(
		ForkModuleCommand command,
		IMediator mediator)
		=> mediator.Send(command);
}
