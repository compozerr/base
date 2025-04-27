using Api.Abstractions;
using MediatR;

namespace Api.Endpoints.Projects.Domains.Get;

public static class GetDomainsRoute
{
    public const string Route = "";

    public static RouteHandlerBuilder AddGetDomainsRoute(this IEndpointRouteBuilder app)
    {
        return app.MapGet(Route, ExecuteAsync);
    }

    public static Task<GetDomainsResponse> ExecuteAsync(
        ProjectId projectId,
        IMediator mediator)
        => mediator.Send(
            new GetDomainsCommand(
                projectId));
}
