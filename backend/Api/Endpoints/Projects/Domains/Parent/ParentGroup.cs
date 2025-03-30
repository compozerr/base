using Api.Endpoints.Projects.Domains.Parent.Get;

namespace Api.Endpoints.Projects.Domains.Parent;

public static class ParentGroup
{
    public const string Route = "{domainId:guid}/parent";

    public static RouteGroupBuilder AddParentGroup(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup(Route);

        group.AddGetParentDomainRoute();

        return group;
    }
}
