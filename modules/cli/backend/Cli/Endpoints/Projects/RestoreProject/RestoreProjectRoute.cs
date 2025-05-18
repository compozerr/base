using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using MediatR;
using Api.Abstractions;

namespace Cli.Endpoints.Projects.RestoreProject;

public static class RestoreProjectRoute
{
	public const string Route = "{projectId:guid}/restore";

	public static RouteHandlerBuilder AddRestoreProjectRoute(this IEndpointRouteBuilder app)
	{
		return app.MapPost(
		    Route,
		    ExecuteAsync);
	}

	public static Task<RestoreProjectResponse> ExecuteAsync(
		ProjectId projectId,
		IMediator mediator)
		=> mediator.Send(
			new RestoreProjectCommand(
				projectId));
}
