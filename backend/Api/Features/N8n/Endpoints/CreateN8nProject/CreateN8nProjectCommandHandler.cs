using Api.Data.Repositories;
using Api.Services;
using Auth.Services;
using Core.MediatR;

namespace Api.Features.N8n.Endpoints.CreateN8nProject;

public sealed record CreateN8nProjectCommandHandler(
    IProjectRepository ProjectRepository,
    ICurrentUserAccessor CurrentUserAccessor,
    ILocationRepository LocationRepository,
    IProjectManager ProjectManager) : ICommandHandler<CreateN8nProjectCommand, CreateN8nProjectResponse>
{
    private const string N8nTemplateRepoUrl = "https://github.com/compozerr/n8n-template";

    public async Task<CreateN8nProjectResponse> Handle(CreateN8nProjectCommand command, CancellationToken cancellationToken = default)
    {
        var userId = CurrentUserAccessor.CurrentUserId!;

        var location = await LocationRepository.GetLocationByIso(command.LocationIso);

        ProjectManager.


        return new CreateN8nProjectResponse(project.Id);
    }
}
