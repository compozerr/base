using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Mail.Endpoints.Admin.ListSentEmails;

public static class ListSentEmailsRoute
{
    public const string Route = "";

    public static RouteHandlerBuilder AddListSentEmailsRoute(this IEndpointRouteBuilder app)
    {
        return app.MapGet(Route, ExecuteAsync);
    }

    public static Task<ListSentEmailsResponse> ExecuteAsync(
        [AsParameters] ListSentEmailsCommand command,
        IMediator mediator)
        => mediator.Send(command);
}
