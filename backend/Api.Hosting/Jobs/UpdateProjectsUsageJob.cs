using Api.Abstractions;
using Api.Data.Repositories;
using Api.Hosting.Services;
using Jobs;
using Serilog;

namespace Api.Hosting.Jobs;

public class UpdateProjectsUsageJob(
    IServerRepository serverRepository,
    IHostingApiFactory hostingApiFactory) : JobBase<UpdateProjectsUsageJob>
{
    public override async Task ExecuteAsync()
    {
        var servers = await serverRepository.GetAllAsync();

        foreach (var server in servers)
        {
            var hostingApi = await hostingApiFactory.GetHostingApiAsync(server.Id);
            var projectsUsage = await hostingApi.GetProjectsUsageAsync();

            Log.Logger.ForContext(nameof(projectsUsage), projectsUsage)
                      .Information("Project usage received");

            if (projectsUsage is null) continue;

        }
    }

    private async Task GetProjectData(ProjectId projectId)
    {

    }
}