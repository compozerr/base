using Api.Abstractions;
using MediatR;

namespace Api.Endpoints.Projects.Project.ChangeTier;

public sealed record ChangeTierRequest(string Tier);

public static class ChangeTierRoute
{
	public const string Route = "{projectId:guid}/change-tier";

	public static RouteHandlerBuilder AddChangeTierRoute(this IEndpointRouteBuilder app)
	{
		return app.MapPut(Route, ExecuteAsync);
	}

	public static Task<ChangeTierResponse> ExecuteAsync(
		ProjectId projectId,
		ChangeTierRequest request,
		IMediator mediator)
		=> mediator.Send(
			new ChangeTierCommand(
				projectId,
				request.Tier));
}
