using Api.Abstractions;
using MediatR;

namespace Api.Endpoints.Projects.Domains.Parent.Get;

public static class GetParentDomainRoute
{
    public const string Route = "";

    public static RouteHandlerBuilder AddGetParentDomainRoute(this IEndpointRouteBuilder app)
    {
        return app.MapGet(Route, ExecuteAsync);
    }

    public static Task<GetParentDomainResponse> ExecuteAsync(
        ProjectId projectId,
        DomainId domainId,
        IMediator mediator)
        => mediator.Send(
            new GetParentDomainCommand(
                domainId));
}
