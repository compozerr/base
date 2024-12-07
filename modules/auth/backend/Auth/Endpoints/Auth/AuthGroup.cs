using Microsoft.AspNetCore.Builder;

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
        app.AddLogoutRoute().RequireAuthorization();
        app.AddWhoAmIRoute().RequireAuthorization();
        app.AddMeRoute().RequireAuthorization();
    }
}