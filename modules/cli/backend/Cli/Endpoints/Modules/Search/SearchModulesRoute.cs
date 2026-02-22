using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Cli.Endpoints.Modules.Search;

public static class SearchModulesRoute
{
    public const string Route = "search";

    public static RouteHandlerBuilder AddSearchModulesRoute(this IEndpointRouteBuilder app)
    {
        return app.MapGet(Route, ExecuteAsync).AllowAnonymous();
    }

    public static Task<SearchModulesResponse> ExecuteAsync(
        [AsParameters] SearchModulesCommand command,
        IMediator mediator)
        => mediator.Send(command);
}
