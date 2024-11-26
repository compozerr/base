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

        return builder.AddGitHub(options => {
            options.ClientId = githubOptions.Value.ClientId;
            options.ClientSecret = githubOptions.Value.ClientSecret;

            // Optional: Request additional scopes
            options.Scope.Add("user:email");
            options.Scope.Add("read:org");

            // Customize claims mapping
            options.ClaimActions.MapJsonKey(ClaimTypes.Email, "email");
            options.ClaimActions.MapJsonKey("urn:github:name", "name");
            options.ClaimActions.MapJsonKey("urn:github:url", "html_url");
            options.ClaimActions.MapJsonKey("urn:github:avatar", "avatar_url");

            // Optional: Save access token for using GitHub API later
            options.SaveTokens = true;

            // Customize authentication events
            options.Events = new Microsoft.AspNetCore.Authentication.OAuth.OAuthEvents
            {
                OnCreatingTicket = async context =>
                {
                    // Get user organizations if scope includes read:org
                    if (context.AccessToken != null && options.Scope.Contains("read:org"))
                    {
                        var githubClient = new HttpClient();
                        githubClient.DefaultRequestHeaders.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));
                        githubClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", context.AccessToken);
                        githubClient.DefaultRequestHeaders.UserAgent.Add(new System.Net.Http.Headers.ProductInfoHeaderValue("YourApp", "1.0"));

                        var orgsResponse = await githubClient.GetAsync("https://api.github.com/user/orgs");
                        if (orgsResponse.IsSuccessStatusCode)
                        {
                            var orgs = await orgsResponse.Content.ReadFromJsonAsync<JsonDocument>();
                            foreach (var org in orgs!.RootElement.EnumerateArray())
                            {
                                var orgLogin = org.GetProperty("login").GetString();
                                context.Identity?.AddClaim(new Claim(ClaimTypes.Role, $"org-{orgLogin}"));
                            }
                        }
                    }
                },
                OnAccessDenied = context =>
                {
                    context.Response.Redirect("/access-denied");
                    context.HandleResponse();
                    return Task.CompletedTask;
                }
            };
        });
    }
}