using Api.Abstractions;
using Core.MediatR;
using MediatR;
using Microsoft.AspNetCore.Routing;

namespace Api.Endpoints.Projects.Domains.SetPrimary;

public sealed record SetPrimaryResponse(
    bool Success);

public static class SetPrimaryRoute
{
    public const string Route = "{domainId:guid}/set-primary";

    public static RouteHandlerBuilder AddSetPrimaryRoute(this IEndpointRouteBuilder app)
    {
        return app.MapPost(Route, ExecuteAsync);
    }

    public static Task<SetPrimaryResponse> ExecuteAsync(
        ProjectId projectId,
        DomainId domainId,
        IMediator mediator)
    {
        return mediator.Send(
            new SetPrimaryCommand(
                domainId));
    }
}
