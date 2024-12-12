using Github.Endpoints.Installation;

namespace Github.Endpoints;

public class GithubGroup : CarterModule
{
    public GithubGroup() : base("/github")
    {
        WithTags(nameof(Github));
    }

    public override void AddRoutes(IEndpointRouteBuilder app)
    {
        app.AddInstallationGroup();
    }
}