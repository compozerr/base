using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using MediatR;

namespace Api.Hosting.Endpoints.VMPooling.InitiatePoolSync;

public static class InitiatePoolSyncRoute
{
	public const string Route = ""; // Set your route path here

	public static RouteHandlerBuilder AddInitiatePoolSyncRoute(this IEndpointRouteBuilder app)
	{
		return app.MapPost(Route, ExecuteAsync);
	}

	public static Task<InitiatePoolSyncResponse> ExecuteAsync(
		InitiatePoolSyncCommand command,
		IMediator mediator)
		=> mediator.Send(command);
}
