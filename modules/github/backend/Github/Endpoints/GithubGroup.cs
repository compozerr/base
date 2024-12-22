using Github.Endpoints.Installation;
using Microsoft.AspNetCore.Builder;

namespace Github.Endpoints;

public class GithubGroup : CarterModule
{
    public const string Route = "github";

    public GithubGroup() : base(Route)
    {
        WithTags(nameof(Github));
    }

    public override void AddRoutes(IEndpointRouteBuilder app)
    {
        app.AddGetInstallAppUrlRoute();
        app.AddGetInstallatonsRoute().RequireAuthorization();
        app.AddSetDefaultInstallationRoute().RequireAuthorization();
        
        app.AddPlaygroundRoute().RequireAuthorization();
    }
}