using Api.Abstractions;
using Api.Data.Repositories;
using Api.Hosting.VMPooling.N8n;
using Api.Services;
using Auth.Services;
using Core.MediatR;

namespace Api.Features.N8n.Endpoints.CreateN8nProject;

public sealed record CreateN8nProjectCommandHandler(
    ICurrentUserAccessor CurrentUserAccessor,
    ILocationRepository LocationRepository,
    IProjectManager ProjectManager) : ICommandHandler<CreateN8nProjectCommand, CreateN8nProjectResponse>
{
    public async Task<CreateN8nProjectResponse> Handle(
        CreateN8nProjectCommand command,
        CancellationToken cancellationToken = default)
    {
        var userId = CurrentUserAccessor.CurrentUserId!;

        var location = await LocationRepository.GetLocationByIso(command.LocationIso);

        var projectId = await ProjectManager.AllocateProjectAsync(
            userId,
            command.ProjectName,
            N8nVMPooler.N8nTemplateRepoUrl,
            ServerTiers.GetById(new ServerTierId(command.Tier)),
            location,
            ProjectType.N8n,
            cancellationToken);

        return new CreateN8nProjectResponse(projectId);
    }
}
