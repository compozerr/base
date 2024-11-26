using AspNet.Security.OAuth.GitHub;
using Carter;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Auth.AuthEndpoints;

public static class LoginRoute
{
    public static void AddLoginRoute(this IEndpointRouteBuilder app)
    {
        app.MapPost("/login", async (HttpContext context) =>
        {
            await context.ChallengeAsync(GitHubAuthenticationDefaults.AuthenticationScheme);
        });
    }
}
