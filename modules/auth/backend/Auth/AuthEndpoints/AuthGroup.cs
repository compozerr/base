using Carter;
using Microsoft.AspNetCore.Routing;

namespace Auth.AuthEndpoints;

public class AuthGroup : CarterModule
{
    public AuthGroup()
    {
        WithTags(nameof(Auth));
    }

    public override void AddRoutes(IEndpointRouteBuilder app)
    {
        app.AddLoginRoute();
        app.AddLogoutRoute();
    }

}