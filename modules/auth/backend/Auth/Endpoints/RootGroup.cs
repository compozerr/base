using Auth.AuthProviders;

namespace Auth.Endpoints;

public class RootGroup : CarterModule
{
    public RootGroup() : base("/")
    {
        WithTags(nameof(Auth));
    }

    public override void AddRoutes(IEndpointRouteBuilder app)
    {
        app.AddGitHubCallbackRoute();
    }
}