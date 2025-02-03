using Auth.Services;
using Github.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;

namespace Cli.Endpoints.Deployment;

public static class GetProjectsRoute
{
    public const string Route = "/projects";

    public static RouteHandlerBuilder AddGetProjectsRoute(this IEndpointRouteBuilder app)
    {
        return app.MapGet(Route, ExecuteAsync);
    }

    public static Task<IReadOnlyList<RepositoryDto>> ExecuteAsync(
        ICurrentUserAccessor currentUserAccessor)
        => throw new NotImplementedException();
}
