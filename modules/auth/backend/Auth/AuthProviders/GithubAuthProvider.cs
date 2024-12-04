using System.Security.Claims;
using AspNet.Security.OAuth.GitHub;
using Auth.Endpoints.Users.Create;
using Core.Extensions;
using MediatR;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.OAuth;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Serilog;

namespace Auth.AuthProviders;

public static class GithubAuthProvider
{
    public static AuthenticationBuilder AddGithubAuthProvider(this AuthenticationBuilder builder)
    {
        builder.Services.AddRequiredConfigurationOptions<GithubOptions>("Auth:Github");

        var githubOptions = builder.Services.BuildServiceProvider().GetRequiredService<IOptions<GithubOptions>>();

        return builder.AddGitHub(options =>
        {
            options.CallbackPath = "/v1/auth/signin-github";
            options.ClientId = githubOptions.Value.ClientId;
            options.ClientSecret = githubOptions.Value.ClientSecret;

            options.Scope.Add("user:email");
            options.Scope.Add("read:org");

            options.ClaimActions.MapJsonKey(ClaimTypes.Email, "email");
            options.ClaimActions.MapJsonKey("urn:github:name", "name");
            options.ClaimActions.MapJsonKey("urn:github:url", "html_url");
            options.ClaimActions.MapJsonKey("urn:github:avatar", "avatar_url");

            options.SaveTokens = true;

            options.Events = new OAuthEvents
            {
                OnTicketReceived = async context =>
                {
                    var mediator = context.HttpContext.RequestServices.GetRequiredService<IMediator>();

                    Log.ForContext("User", context.Principal!.Identity?.Name)
                       .Information("Received GitHub authentication ticket");

                    await AddUser(mediator, context.Principal!);
                },
            };
        });
    }

    public static async Task AddUser(IMediator mediator, ClaimsPrincipal principal)
    {
        var email = principal.FindFirst(ClaimTypes.Email)?.Value;
        var avatarUrl = principal.FindFirst("urn:github:avatar")?.Value;

        if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(avatarUrl))
            return;

        var command = new CreateUserCommand(
            Email: email,
            AvatarUrl: avatarUrl
        );

        await mediator.Send(command);
    }
}