using System.Net.Http.Json;
using System.Web;
using Core.Helpers;
using Github.Endpoints.Installation.Upsert;
using Github.Helpers;
using Github.Options;
using Github.Services;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Github.Endpoints.Installation;

public static class InstallationCallbackRoute
{
    public const string Route = "callback";

    public static string CallbackPath => $"v1/{GithubGroup.Route}/{InstallationGroup.Route}/{Route}";

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

            var rawResponse = await response.Content.ReadAsStringAsync(cancellationToken);

            var deserializedResponse = AccessTokenCallbackResponseParser.ParseResponse(rawResponse);

            if(!deserializedResponse.IsSuccess)
            {
                return Results.BadRequest();
            }

            var content = deserializedResponse.Success!;

            var mediatr = context.RequestServices.GetRequiredService<IMediator>();

            var upsertInstallationCommand = new UpsertInstallationCommand(deserializedState.UserId, content.AccessToken, content.Scope);

            var installationId = await mediatr.Send(upsertInstallationCommand, cancellationToken);

            return Results.Ok(installationId);
        });
    }
}
