using Auth.Services;
using Github.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;

namespace Cli.Endpoints.Deployment;

public static class DeployProjectRoute
{
    public const string Route = "/projects";

    public static RouteHandlerBuilder AddDeployProjectRoute(this IEndpointRouteBuilder app)
    {
        return app.MapPost(Route, ExecuteAsync);
    }

    public static Task<IReadOnlyList<RepositoryDto>> ExecuteAsync(
        ICurrentUserAccessor currentUserAccessor)
        => throw new NotImplementedException();
}
