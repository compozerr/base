using Api.Endpoints.Projects.Environments.ChangeAutoDeploy;
using Api.Endpoints.Projects.Environments.ChangeTier;
using Api.Endpoints.Projects.Environments.Create;
using Api.Endpoints.Projects.Environments.Delete;
using Api.Endpoints.Projects.Environments.Deployments;
using Api.Endpoints.Projects.Environments.Domains;
using Api.Endpoints.Projects.Environments.Get;
using Api.Endpoints.Projects.Environments.GetBranches;
using Api.Endpoints.Projects.Environments.Promote;
using Api.Endpoints.Projects.Environments.Services;
using Api.Endpoints.Projects.Environments.Services.GetServiceLogs;
using Api.Endpoints.Projects.Environments.UpdateBranches;
using Api.Endpoints.Projects.Environments.Variables;

namespace Api.Endpoints.Projects.Environments;

public static class EnvironmentsGroup
{
    public const string Route = "{projectId:guid}/environments";

    public static RouteGroupBuilder AddEnvironmentsGroup(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup(Route);

        group.AddGetEnvironmentsRoute();
        group.AddGetBranchesRoute();
        group.AddCreateEnvironmentRoute();
        group.AddDeleteEnvironmentRoute();
        group.AddPromoteEnvironmentRoute();
        group.AddChangeEnvironmentTierRoute();
        group.AddChangeEnvironmentAutoDeployRoute();
        group.AddUpdateEnvironmentBranchesRoute();
        group.AddGetEnvironmentDeploymentsRoute();
        group.AddGetEnvironmentServicesRoute();
        group.AddGetServiceLogsRoute();
        group.AddGetEnvironmentDomainsRoute();
        group.AddEnvironmentVariablesGroup();

        return group;
    }
}
