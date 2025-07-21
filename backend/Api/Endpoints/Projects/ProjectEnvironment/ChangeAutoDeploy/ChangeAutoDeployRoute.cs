using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using MediatR;
using Api.Abstractions;

namespace Api.Endpoints.Projects.ProjectEnvironment.ChangeAutoDeploy;

public sealed record ChangeAutoDeployRequest(
	bool AutoDeploy);

public static class ChangeAutoDeployRoute
{
	public const string Route = "change-auto-deploy"; // Set your route path here

	public static RouteHandlerBuilder AddChangeAutoDeployRoute(this IEndpointRouteBuilder app)
	{
		return app.MapPut(Route, ExecuteAsync);
	}

	public static Task<ChangeAutoDeployResponse> ExecuteAsync(
		ProjectId projectId,
		ChangeAutoDeployRequest command,
		IMediator mediator)
		=> mediator.Send(new ChangeAutoDeployCommand(
			projectId,
			command.AutoDeploy));
}
