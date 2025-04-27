using System.Diagnostics.CodeAnalysis;
using Api.Data;
using Api.Data.Repositories;
using Api.Hosting.Dtos;
using Api.Hosting.Services;
using Core.Extensions;
using Jobs;
using Serilog;

namespace Api.Hosting.Jobs;

public class UpdateProjectsUsageJob(
    IServerRepository serverRepository,
    IProjectRepository projectRepository,
    IProjectUsageRepository projectUsageRepository,
    IHostingApiFactory hostingApiFactory) : JobBase<UpdateProjectsUsageJob>
{
    public override async Task ExecuteAsync()
    {
        var servers = await serverRepository.GetAllAsync();

        foreach (var server in servers)
        {
            var hostingApi = await hostingApiFactory.GetHostingApiAsync(server.Id);
            var projectUsageDtos = await hostingApi.GetProjectsUsageAsync();

            if (projectUsageDtos is null)
            {
                Log.Warning("No project usage data received for server {ServerId}", server.Id);
                continue;
            }

            var projectUsages = projectUsageDtos.ToEntities();

            var matchingProjects = await projectRepository.GetAllAsync(x => x.Where(y => projectUsages.Select(p => p.ProjectId).Contains(y.Id)));

            var projectUsagesWithMatchingProject = projectUsages.Where(x => matchingProjects.Select(y => y.Id).Contains(x.ProjectId)).ToList();

            var missingProjects = projectUsages.Where(x => !matchingProjects.Select(y => y.Id).Contains(x.ProjectId)).ToList();

            if (missingProjects.Count > 0)
            {
                Log.ForContext(nameof(missingProjects), missingProjects.Select(x => x.ProjectId))
                   .Error("Missing project(s)");
            }

            await projectUsageRepository.AddProjectUsages(projectUsagesWithMatchingProject);

            await matchingProjects.ApplyAsync(x => projectRepository.SetProjectStateAsync(x.Id, GetProjectState(x, projectUsagesWithMatchingProject)));

            Log.ForContext("serverId", server.Id.Value)
               .ForContext("projectUsagesCount", projectUsagesWithMatchingProject.Count)
               .Information("Processed server project usages");
        }
    }

    private static ProjectState GetProjectState(Project project, List<ProjectUsage> projectUsages)
    {
        return project.State switch
        {
            ProjectState.Starting => ProjectState.Starting,
            ProjectState.Deleting => ProjectState.Deleting,
            _ => projectUsages.Where(p => p.ProjectId == project.Id)
                              .OrderByDescending(p => p.CreatedAtUtc)
                              .FirstOrDefault()?.Status == ProjectStatus.Running ? ProjectState.Running : ProjectState.Stopped
        };
    }
}