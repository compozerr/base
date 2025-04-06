using Api.Endpoints.Projects.Domains.Add;
using Api.Endpoints.Projects.Domains.Delete;
using Api.Endpoints.Projects.Domains.Get;
using Api.Endpoints.Projects.Domains.Parent;
using Api.Endpoints.Projects.Domains.SetPrimary;
using Api.Endpoints.Projects.Domains.Verify;

namespace Api.Endpoints.Projects.Domains;

public static class DomainsGroup
{
    public const string Route = "{projectId:guid}/domains";

    public static RouteGroupBuilder AddDomainsGroup(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup(Route);

        group.AddGetDomainsRoute();
        group.AddDeleteDomainRoute();
        group.AddAddDomainRoute();
        group.AddGetVerifyRoute();
        group.AddSetPrimaryRoute();

        group.AddParentGroup();

        return group;
    }
}
