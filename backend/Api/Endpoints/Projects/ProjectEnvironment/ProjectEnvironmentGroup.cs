namespace Api.Endpoints.Projects.ProjectEnvironment;

// Legacy branch-based env lookup. Retained only as a back-compat shim for
// hosting/manager's docker-build-service, which still fetches env vars by
// branch during build. Remove once hosting is migrated to the by-id endpoint.
public static class ProjectEnvironmentGroup
{
    public const string Route = "{projectId:guid}/environment";

    public static RouteGroupBuilder AddProjectEnvironmentGroup(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup(Route);

        group.AddGetProjectEnvironmentRoute();

        return group;
    }
}
