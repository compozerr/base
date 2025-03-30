namespace Api.Endpoints.Projects.Domains;

public static class DomainsGroup
{
    public const string Route = "domains";

    public static RouteGroupBuilder AddDomainsGroup(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup(Route);

        return group;
    }
}
