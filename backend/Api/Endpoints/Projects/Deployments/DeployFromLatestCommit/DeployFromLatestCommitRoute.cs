using MediatR;
using Api.Abstractions;

namespace Api.Endpoints.Projects.Deployments.DeployFromLatestCommit;

public static class DeployFromLatestCommitRoute
{
	public const string Route = "deploy-from-latest-commit";

	public static RouteHandlerBuilder AddDeployFromLatestCommitRoute(this IEndpointRouteBuilder app)
	{
		return app.MapPost(Route, ExecuteAsync);
	}

	public static Task<DeployFromLatestCommitResponse> ExecuteAsync(
		ProjectId projectId,
		IMediator mediator,
		Guid? environmentId = null)
		=> mediator.Send(
		    new DeployFromLatestCommitCommand(
		        projectId,
		        environmentId.HasValue ? new ProjectEnvironmentId(environmentId.Value) : null));
}
