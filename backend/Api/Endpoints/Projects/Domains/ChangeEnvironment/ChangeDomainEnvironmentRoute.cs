using Api.Abstractions;
using MediatR;

namespace Api.Endpoints.Projects.Domains.ChangeEnvironment;

public sealed record ChangeDomainEnvironmentRequest(Guid? EnvironmentId);

public static class ChangeDomainEnvironmentRoute
{
    public const string Route = "{domainId:guid}/environment";

    public static RouteHandlerBuilder AddChangeDomainEnvironmentRoute(this IEndpointRouteBuilder app)
    {
        return app.MapPatch(Route, ExecuteAsync);
    }

    public static Task<ChangeDomainEnvironmentResponse> ExecuteAsync(
        ProjectId projectId, DomainId domainId,
        ChangeDomainEnvironmentRequest request, IMediator mediator)
        => mediator.Send(new ChangeDomainEnvironmentCommand(projectId, domainId,
            request.EnvironmentId is { } envId ? new ProjectEnvironmentId(envId) : null));
}
