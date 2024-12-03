using System.Security.Claims;
using AspNet.Security.OAuth.GitHub;
using Auth.Endpoints.Users.Create;
using Core.Extensions;
using MediatR;
using Microsoft.AspNetCore.Authentication;
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
        });
    }

    public static RouteHandlerBuilder AddGitHubCallbackRoute(this IEndpointRouteBuilder app)
    {
        return app.MapGet("/signin-github", async (
            HttpContext context,
            IMediator mediator) =>
        {
            try
            {
                // Get the authentication result
                var result = await context.AuthenticateAsync(GitHubAuthenticationDefaults.AuthenticationScheme);

                if (!result?.Succeeded ?? true)
                {
                    return Results.Unauthorized();
                }

                // Extract user info from GitHub claims
                var email = result?.Principal?.FindFirst(ClaimTypes.Email)?.Value;
                var avatarUrl = result?.Principal?.FindFirst("urn:github:avatar")?.Value;

                if (string.IsNullOrEmpty(email))
                {
                    return Results.BadRequest("Email not provided by GitHub");
                }

                // Create user command
                var command = new CreateUserCommand(
                    Email: email,
                    AvatarUrl: avatarUrl ?? string.Empty
                );

                // Send command to create user
                var response = await mediator.Send(command);

                Log.ForContext("UserId", response.Id)
                   .ForContext("Email", email)
                   .Information("User created successfully after GitHub authentication");

                // Sign in the user
                if (result?.Principal != null)
                    await context.SignInAsync(result.Principal);

                // Redirect to the return URL from the authentication properties
                var returnUrl = result?.Properties?.RedirectUri ?? "/";
                return Results.Redirect(returnUrl);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error processing GitHub callback");
                return Results.Problem(
                    title: "Authentication Failed",
                    detail: "Unable to process GitHub authentication",
                    statusCode: 500
                );
            }
        });
    }
}