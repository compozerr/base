using Api.Abstractions;
using MediatR;

namespace Api.Endpoints.Projects.Environments.ChangeTier;

public static class ChangeEnvironmentTierRoute
{
    public const string Route = "{environmentId:guid}/tier";

    public static RouteHandlerBuilder AddChangeEnvironmentTierRoute(this IEndpointRouteBuilder app)
    {
        return app.MapPut(Route, ExecuteAsync);
    }

    public static Task<ChangeEnvironmentTierResponse> ExecuteAsync(
        ProjectId projectId,
        ProjectEnvironmentId environmentId,
        ChangeEnvironmentTierRequest request,
        IMediator mediator)
        => mediator.Send(new ChangeEnvironmentTierCommand(projectId, environmentId, request.Tier));
}

public sealed record ChangeEnvironmentTierRequest(string Tier);
