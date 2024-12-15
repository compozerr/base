using System.Web;
using Auth.Services;
using Core.Helpers;
using Github.Options;
using Github.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;

namespace Github.Endpoints.Installation;

public static class GetInstallationAuthorizeUrlRoute
{
    public const string Route = "get-installation-authorize-url";
    public static RouteHandlerBuilder AddGetInstallationAuthorizeUrlRoute(this IEndpointRouteBuilder app)
    {
        return app.MapGet(Route, (
            IOptions<GithubAppOptions> options,
            IMyServerUrlAccessor myServerUrlAccessor,
            ICurrentUserAccessor userAccessor,
            IGithubJsonWebTokenService jwtService,
            IStateService stateService) =>
        {

            var uriBuilder = new UriBuilder("https://github.com/login/oauth/authorize");

            var query = HttpUtility.ParseQueryString(uriBuilder.Query);

            query["client_id"] = options.Value.ClientId;
            query["redirect_uri"] = myServerUrlAccessor.CombineWithPath(InstallationCallbackRoute.CallbackPath).ToString();
            query["state"] = stateService.Serialize(new InstallationState(UserId: userAccessor.CurrentUserId!));

            uriBuilder.Query = query.ToString();

            return Results.Ok(uriBuilder.ToString());
        });
    }
}