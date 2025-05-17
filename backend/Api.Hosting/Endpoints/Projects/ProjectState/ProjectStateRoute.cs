using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using MediatR;
using Api.Abstractions;

namespace Api.Hosting.Endpoints.Projects.ProjectState;

public sealed record ProjectStateRequest(
	Data.ProjectState State);

public static class ProjectStateRoute
{
	public const string Route = "{projectId:guid}/state"; 

	public static RouteHandlerBuilder AddProjectStateRoute(this IEndpointRouteBuilder app)
	{
		return app.MapPut(Route, ExecuteAsync);
	}

	public static Task<ProjectStateResponse> ExecuteAsync(
		ProjectId projectId,
		ProjectStateRequest request,
		IMediator mediator)
		=> mediator.Send(
			new ProjectStateCommand(
				projectId,
				request.State));
}
