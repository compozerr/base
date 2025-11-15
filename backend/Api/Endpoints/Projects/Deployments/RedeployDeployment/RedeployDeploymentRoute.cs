using Api.Abstractions;
using MediatR;

namespace Api.Endpoints.Projects.Deployments.RedeployDeployment;

public static class RedeployDeploymentRoute
{
	public const string Route = "{deploymentId:guid}/redeploy"; // Set your route path here

	public static RouteHandlerBuilder AddRedeployDeploymentRoute(this IEndpointRouteBuilder app)
	{
		return app.MapPost(Route, ExecuteAsync);
	}

	public static Task<RedeployDeploymentResponse> ExecuteAsync(
		ProjectId projectId,
		DeploymentId deploymentId,
		IMediator mediator)
		=> mediator.Send(new RedeployDeploymentCommand(
			deploymentId));
}
