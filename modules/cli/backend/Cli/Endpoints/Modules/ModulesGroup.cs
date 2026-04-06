using Cli.Endpoints.Modules.Add;
using Cli.Endpoints.Modules.ForkModule;
using Cli.Endpoints.Modules.Info;
using Cli.Endpoints.Modules.List;
using Cli.Endpoints.Modules.Release;
using Cli.Endpoints.Modules.Search;
using Cli.Endpoints.Modules.SyncStatus;
using Cli.Endpoints.Modules.TrackInstall;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;

namespace Cli.Endpoints.Modules;

public static class ModuleGroup
{
    public const string Route = "modules";

    public static RouteGroupBuilder AddModuleGroup(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup(Route);

        group.AddAddModuleRoute();
        group.AddForkModuleRoute();
        group.AddListMyModulesRoute();
        group.AddReleaseModuleRoute();
        group.AddSearchModulesRoute();
        group.AddGetModuleInfoRoute();
        group.AddGetModuleSyncStatusRoute();
        group.AddTrackInstallRoute();

        return group;
    }
}
