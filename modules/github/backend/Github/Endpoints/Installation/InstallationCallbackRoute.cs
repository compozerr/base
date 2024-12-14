using System.Net.Http.Json;
using System.Security.Cryptography.Pkcs;
using System.Web;
using Core.Helpers;
using Github.Endpoints.Installation.Upsert;
using Github.Options;
using Github.Services;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Newtonsoft.Json.Serialization;

namespace Github.Endpoints.Installation;

public sealed record CallbackResponse(string AccessToken, string TokenType, string Scope);

public static class InstallationCallbackRoute
{
    public const string Route = "callback";

    public static string CallbackPath => $"{GithubGroup.Route}/${InstallationGroup.Route}/${Route}";

    public static RouteHandlerBuilder AddInstallationCallbackRoute(this IEndpointRouteBuilder app)
    {
        return app.MapGet(Route, async (
            string code,
            string state,
            HttpContext context,
            IHttpClientFactory httpClientFactory,
            IOptions<GithubAppOptions> options,
            IMyServerUrlAccessor myServerUrlAccessor,
            IStateService stateService,
            CancellationToken cancellationToken) =>
        {

            using var client = httpClientFactory.CreateClient();

            var deserializedState = stateService.Deserialize<InstallationState>(state);

            var uriBuilder = new UriBuilder("https://github.com/login/oauth/access_token");

            var query = HttpUtility.ParseQueryString(uriBuilder.Query);

            query["client_id"] = options.Value.ClientId;
            query["client_secret"] = options.Value.ClientSecret;
            query["code"] = code;
            query["redirect_uri"] = myServerUrlAccessor.CombineWithPath(CallbackPath).ToString();

            uriBuilder.Query = query.ToString();

            var url = uriBuilder.ToString();


            var response = await client.PostAsync(url, null, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                return Results.BadRequest();
            }

            var content = await response.Content.ReadFromJsonAsync<CallbackResponse>(new System.Text.Json.JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
            }, cancellationToken);

            if (content is null)
            {
                return Results.BadRequest();
            }

            var mediatr = context.RequestServices.GetRequiredService<IMediator>();

            var upsertInstallationCommand = new UpsertInstallationCommand(deserializedState.UserId, content.AccessToken, content.Scope);

            var installationId = await mediatr.Send(upsertInstallationCommand, cancellationToken);

            return Results.Ok(installationId);
        });
    }
}