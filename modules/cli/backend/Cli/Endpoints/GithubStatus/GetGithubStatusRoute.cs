using Auth.Services;
using Github.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Cli.Endpoints.GithubStatus;

public static class GetGithubStatusRoute
{
    public const string Route = "/status";

    public static RouteHandlerBuilder AddGetGithubStatusRoute(this IEndpointRouteBuilder app)
    {
        return app.MapGet(Route, ExecuteAsync);
    }

    public static async Task<IResult> ExecuteAsync(
        ICurrentUserAccessor currentUserAccessor,
        IGithubService githubService)
    {
        var userId = currentUserAccessor.CurrentUserId!;
        var userLogin = await githubService.GetUserLoginAsync(userId);

        if (userLogin?.AccessToken is null)
        {
            return Results.Ok(new GithubStatusResponse(
                IsConnected: false,
                IsValid: false,
                Message: "GitHub not connected. Please connect GitHub in your settings."
            ));
        }

        var isValid = await githubService.ValidateTokenAsync(userLogin.AccessToken);

        return Results.Ok(new GithubStatusResponse(
            IsConnected: true,
            IsValid: isValid,
            Message: isValid ? null : "GitHub token expired or revoked. Please reconnect GitHub."
        ));
    }
}

public record GithubStatusResponse(
    bool IsConnected,
    bool IsValid,
    string? Message
);
