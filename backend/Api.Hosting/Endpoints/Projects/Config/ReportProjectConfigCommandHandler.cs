using Api.Data.Repositories;
using Core.MediatR;
using Serilog;

namespace Api.Hosting.Endpoints.Projects.Config;

public sealed class ReportProjectConfigCommandHandler(
    IProjectRepository projectRepository) : ICommandHandler<ReportProjectConfigCommand, ReportProjectConfigResponse>
{
    public async Task<ReportProjectConfigResponse> Handle(
        ReportProjectConfigCommand command,
        CancellationToken cancellationToken = default)
    {
        var project = await projectRepository.GetByIdAsync(command.ProjectId);

        if (project is null)
        {
            Log.ForContext("projectId", command.ProjectId)
                .Warning("Project not found when reporting config");
            return new ReportProjectConfigResponse();
        }

        if (project.Name != command.Name)
        {
            Log.ForContext("projectId", command.ProjectId)
                .ForContext("oldName", project.Name)
                .ForContext("newName", command.Name)
                .Information("Project name updated from compozerr.json");

            project.Name = command.Name;
            await projectRepository.UpdateAsync(project);
        }

        return new ReportProjectConfigResponse();
    }
}
