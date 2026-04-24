using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Routing;

namespace Mail.Endpoints.Admin.GetSentEmail;

public static class GetSentEmailRoute
{
    public const string Route = "{id:guid}";

    public static RouteHandlerBuilder AddGetSentEmailRoute(this IEndpointRouteBuilder app)
    {
        return app.MapGet(Route, ExecuteAsync);
    }

    public static async Task<Results<Ok<GetSentEmailResponse>, NotFound>> ExecuteAsync(
        Guid id,
        IMediator mediator)
    {
        var response = await mediator.Send(new GetSentEmailCommand(SentEmailId.Create(id)));
        return response is null ? TypedResults.NotFound() : TypedResults.Ok(response);
    }
}
