using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Auth.AuthEndpoints;

public static class WhoAmIRoute
{
    public static void AddWhoAmIRoute(this IEndpointRouteBuilder app)
    {
        app.MapGet("/whoami", [Authorize] (ClaimsPrincipal user) =>
        {
            var claims = user.Claims.GroupBy(c => c.Type)
                                .ToDictionary(
                                    g => g.Key.Split('/').Last(), // Get the last part of the claim type
                                    g => g.Count() == 1
                                        ? (object)g.First().Value
                                        : g.Select(c => c.Value).ToList()
                                );

            // GitHub-specific claims with friendly names
            var githubClaims = new
            {
                Name = user.FindFirst("urn:github:name")?.Value,
                Url = user.FindFirst("urn:github:url")?.Value,
                Avatar = user.FindFirst("urn:github:avatar")?.Value
            };

            // Get all roles
            var roles = user.Claims
                .Where(c => c.Type == ClaimTypes.Role)
                .Select(c => c.Value)
                .ToList();

            return Results.Ok(new
            {
                Identity = new
                {
                    IsAuthenticated = user.Identity?.IsAuthenticated ?? false,
                    user.Identity?.AuthenticationType,
                    user.Identity?.Name
                },
                Roles = roles,
                GitHub = githubClaims,
                Claims = claims,
                // Include raw claims for debugging
                RawClaims = user.Claims.Select(c => new
                {
                    c.Type,
                    c.Value,
                    c.Issuer
                })
            });
        });
    }
}
