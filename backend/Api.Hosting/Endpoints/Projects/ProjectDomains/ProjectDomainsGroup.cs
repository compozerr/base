using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;

namespace Api.Hosting.Endpoints.Projects.ProjectDomains;

public static class ProjectDomainsGroup
{
    public const string Route = "{projectId:guid}/domains";

    public static RouteGroupBuilder AddProjectDomainsGroup(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup(Route);

        group.AddGetDomainsRoute();

        return group;
    }
}
