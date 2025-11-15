using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using MediatR;
using Api.Abstractions;

namespace Api.Endpoints.Projects.Deployments.DeployFromLatestCommit;

public static class DeployFromLatestCommitRoute
{
	public const string Route = "deploy-from-latest-commit"; // Set your route path here

	public static RouteHandlerBuilder AddDeployFromLatestCommitRoute(this IEndpointRouteBuilder app)
	{
		return app.MapPost(Route, ExecuteAsync);
	}

	public static Task<DeployFromLatestCommitResponse> ExecuteAsync(
		ProjectId projectId,
		IMediator mediator)
		=> mediator.Send(
		    new DeployFromLatestCommitCommand(
		        projectId));
}
