using Api.Abstractions;
using Core.MediatR;
using MediatR;
using Microsoft.AspNetCore.Routing;

namespace Api.Endpoints.Projects.Domains.SetPrimary;

public sealed record SetPrimaryResponse(
    bool Success);

public static class SetPrimaryRoute
{
    public const string Route = "{domainId:guid}";

    public static RouteHandlerBuilder AddSetPrimaryRoute(this IEndpointRouteBuilder app)
    {
        return app.MapPost(Route, ExecuteAsync);
    }

    public static Task<SetPrimaryResponse> ExecuteAsync(
        Guid projectId,
        Guid domainId,
        IMediator mediator)
    {
        return mediator.Send(
            new SetPrimaryCommand(
                DomainId.Create(domainId)));
    }
}
