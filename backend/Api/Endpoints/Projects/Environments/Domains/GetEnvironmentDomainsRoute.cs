using Api.Abstractions;
using Api.Endpoints.Projects.Domains.Get;
using MediatR;

namespace Api.Endpoints.Projects.Environments.Domains;

public static class GetEnvironmentDomainsRoute
{
    public const string Route = "{environmentId:guid}/domains";

    public static RouteHandlerBuilder AddGetEnvironmentDomainsRoute(this IEndpointRouteBuilder app)
    {
        return app.MapGet(Route, ExecuteAsync);
    }

    public static Task<GetDomainsResponse> ExecuteAsync(
        ProjectId projectId,
        ProjectEnvironmentId environmentId,
        IMediator mediator)
        => mediator.Send(
            new GetEnvironmentDomainsCommand(
                projectId,
                environmentId));
}
