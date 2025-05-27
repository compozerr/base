using Github.Endpoints.Installation;
using Github.Options;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Octokit.Webhooks.AspNetCore;

namespace Github.Endpoints;

public class GithubGroup : CarterModule
{
    public const string Route = "github";

    public GithubGroup() : base(Route)
    {
        WithTags(nameof(Github));
    }

    public override void AddRoutes(IEndpointRouteBuilder app)
    {
        app.AddGetInstallAppUrlRoute();
        app.AddGetInstallatonsRoute().RequireAuthorization();
        app.AddSetDefaultInstallationRoute().RequireAuthorization();

        app.AddPlaygroundRoute().RequireAuthorization();

        var githubOptions = app.ServiceProvider.GetRequiredService<IOptions<GithubAppOptions>>();

        app.MapGitHubWebhooks("webhooks", githubOptions.Value.WebhookSecret);
    }
}