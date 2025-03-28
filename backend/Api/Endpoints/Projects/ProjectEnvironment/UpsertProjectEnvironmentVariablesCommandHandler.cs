using Api.Data.Repositories;
using Core.MediatR;

namespace Api.Endpoints.Projects.ProjectEnvironment;

public sealed record UpsertProjectEnvironmentVariablesCommandHandler(
    IProjectRepository ProjectRepository,
    IProjectEnvironmentRepository ProjectEnvironmentRepository) : ICommandHandler<UpsertProjectEnvironmentVariablesCommand, UpsertProjectEnvironmentVariablesResponse>
{
    public async Task<UpsertProjectEnvironmentVariablesResponse> Handle(
        UpsertProjectEnvironmentVariablesCommand command,
        CancellationToken cancellationToken = default)
    {

        var environment = (await ProjectRepository.GetProjectEnvironmentByBranchAsync(
            command.ProjectId,
            command.Branch))!;

        environment.ProjectEnvironmentVariables.ApplyVariableChanges(command.Variables);

        await ProjectEnvironmentRepository.UpdateAsync(environment, cancellationToken);

        return new UpsertProjectEnvironmentVariablesResponse();
    }
}
