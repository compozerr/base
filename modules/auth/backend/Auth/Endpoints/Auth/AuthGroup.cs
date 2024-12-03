using Auth.AuthProviders;
using Carter;
using Microsoft.AspNetCore.Routing;

namespace Auth.Endpoints.Auth;

public class AuthGroup : CarterModule
{
    public AuthGroup() : base("/auth")
    {
        WithTags(nameof(Auth));
    }

    public override void AddRoutes(IEndpointRouteBuilder app)
    {
        app.AddLoginRoute();
        app.AddLogoutRoute();
        app.AddWhoAmIRoute();
        app.AddGitHubCallbackRoute();
    }

}