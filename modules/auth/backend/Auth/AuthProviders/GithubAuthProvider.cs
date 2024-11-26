using System.Net.Http.Json;
using System.Security.Claims;
using System.Text.Json;
using Core.Extensions;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Auth.AuthProviders;

public static class GithubAuthProvider
{
    public static AuthenticationBuilder AddGithubAuthProvider(this AuthenticationBuilder builder)
    {
        builder.Services.AddRequiredConfigurationOptions<GithubOptions>("Auth:Github");

        var githubOptions = builder.Services.BuildServiceProvider().GetRequiredService<IOptions<GithubOptions>>();

        return builder.AddGitHub(options =>
        {
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
}