using Microsoft.AspNetCore.Builder;

namespace Github.Endpoints.Installation;

public static class InstallationGroup
{
    public const string Route = "installation";
    public static IEndpointRouteBuilder AddInstallationGroup(this IEndpointRouteBuilder app)
    {
        var installationGroup = app.MapGroup(Route);

        installationGroup.AddGetInstallationAuthorizeUrlRoute()
                         .RequireAuthorization();

        installationGroup.AddInstallationCallbackRoute();

        return installationGroup;
    }
}