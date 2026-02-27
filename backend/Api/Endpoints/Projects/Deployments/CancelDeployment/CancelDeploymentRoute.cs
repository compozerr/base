using Api.Abstractions;
using MediatR;

namespace Api.Endpoints.Projects.Deployments.CancelDeployment;

public static class CancelDeploymentRoute
{
	public const string Route = "{deploymentId:guid}/cancel";

	public static RouteHandlerBuilder AddCancelDeploymentRoute(this IEndpointRouteBuilder app)
	{
		return app.MapPost(Route, ExecuteAsync);
	}

	public static Task<CancelDeploymentResponse> ExecuteAsync(
		ProjectId projectId,
		DeploymentId deploymentId,
		IMediator mediator)
		=> mediator.Send(new CancelDeploymentCommand(projectId, deploymentId));
}
