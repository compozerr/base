using System.Security.Claims;
using Auth.Endpoints.Auth.UserAuthenticated;
using Core.Extensions;
using MediatR;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.OAuth;
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
            options.Scope.Add("admin:org");

            options.ClaimActions.MapJsonKey(ClaimTypes.Email, "email");
            options.ClaimActions.MapJsonKey("urn:github:name", "name");
            options.ClaimActions.MapJsonKey("urn:github:url", "html_url");
            options.ClaimActions.MapJsonKey("urn:github:avatar", "avatar_url");

            options.SaveTokens = true;

            options.Events = new OAuthEvents
            {
                OnRedirectToAuthorizationEndpoint = context =>
                {
                    // Only redirect if this is the initial login attempt
                    if (context.Request.Path == "/v1/auth/login")
                    {
                        context.Response.Redirect(context.RedirectUri);
                    }
                    else
                    {
                        context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                    }
                    return Task.CompletedTask;
                },
                OnAccessDenied = context =>
                {
                    Log.ForContext("User", context.HttpContext.User.Identity?.Name)
                       .Information("Access denied");

                    context.Response.StatusCode = StatusCodes.Status403Forbidden;
                    return Task.CompletedTask;
                },
                OnTicketReceived = async context =>
                {
                    Log.ForContext("User", context.Principal!.Identity?.Name)
                       .Information("Received GitHub authentication ticket");

                    var mediator = context.HttpContext.RequestServices.GetRequiredService<IMediator>();

                    var userId = await mediator.Send(new UserAuthenticatedCommand(
                        context.Principal,
                        new(context.Properties)));

                    var identity = (ClaimsIdentity)context.Principal.Identity!;
                    identity.RemoveClaim(identity.FindFirst(ClaimTypes.NameIdentifier));
                    identity.AddClaim(new Claim(ClaimTypes.NameIdentifier, userId.ToString()));

                    Log.ForContext("UserId", userId)
                       .Information("User authenticated successfully");
                },
            };
        });
    }
}