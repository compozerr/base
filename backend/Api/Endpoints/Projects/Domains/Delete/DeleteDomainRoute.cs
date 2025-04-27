using Api.Abstractions;
using MediatR;

namespace Api.Endpoints.Projects.Domains.Delete;

public static class DeleteDomainRoute
{
    public const string Route = "{domainId:guid}";

    public static RouteHandlerBuilder AddDeleteDomainRoute(this IEndpointRouteBuilder app)
    {
        return app.MapDelete(Route, ExecuteAsync);
    }

    public static Task<DeleteDomainResponse> ExecuteAsync(
        ProjectId projectId,
        DomainId domainId,
        IMediator mediator)
        => mediator.Send(
            new DeleteDomainCommand(
                domainId));
}
