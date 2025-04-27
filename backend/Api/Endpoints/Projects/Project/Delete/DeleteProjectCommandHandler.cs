using Api.Data.Repositories;
using Core.MediatR;

namespace Api.Endpoints.Projects.Project.Delete;

public sealed class DeleteProjectCommandHandler(
    IProjectRepository projectRepository) : ICommandHandler<DeleteProjectCommand, DeleteProjectResponse>
{
    public async Task<DeleteProjectResponse> Handle(DeleteProjectCommand command, CancellationToken cancellationToken = default)
    {
        
        return new DeleteProjectResponse();
    }
}
