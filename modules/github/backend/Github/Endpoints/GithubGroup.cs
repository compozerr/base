using Github.Endpoints.Installation;

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
        app.AddInstallationGroup();
    }
}