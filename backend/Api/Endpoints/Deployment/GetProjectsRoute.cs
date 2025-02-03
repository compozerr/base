using Auth.Services;
using Github.Services;

namespace Api.Endpoints.Deployment;

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
