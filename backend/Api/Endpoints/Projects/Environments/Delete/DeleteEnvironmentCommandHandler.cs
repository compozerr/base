using Api.Data.Repositories;
using Core.MediatR;

namespace Api.Endpoints.Projects.Environments.Delete;

public sealed class DeleteEnvironmentCommandHandler(
    IEnvironmentRepository environmentRepository) : ICommandHandler<DeleteEnvironmentCommand, DeleteEnvironmentResponse>
{
    public async Task<DeleteEnvironmentResponse> Handle(
        DeleteEnvironmentCommand command,
        CancellationToken cancellationToken = default)
    {
        await environmentRepository.DeleteAsync(
            command.EnvironmentId,
            cancellationToken);

        return new();
    }
}
