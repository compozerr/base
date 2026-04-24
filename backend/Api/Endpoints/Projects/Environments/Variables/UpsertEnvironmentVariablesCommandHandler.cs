using Api.Data.Repositories;
using Api.Endpoints.Projects.ProjectEnvironment;
using Core.MediatR;

namespace Api.Endpoints.Projects.Environments.Variables;

public sealed class UpsertEnvironmentVariablesCommandHandler(
    IProjectEnvironmentRepository projectEnvironmentRepository)
    : ICommandHandler<UpsertEnvironmentVariablesCommand, UpsertEnvironmentVariablesResponse>
{
    public async Task<UpsertEnvironmentVariablesResponse> Handle(
        UpsertEnvironmentVariablesCommand command,
        CancellationToken cancellationToken = default)
    {
        var environment = await projectEnvironmentRepository.GetByIdWithVariablesAsync(command.EnvironmentId)
            ?? throw new InvalidOperationException($"Environment {command.EnvironmentId} not found.");

        if (environment.ProjectId != command.ProjectId)
            throw new InvalidOperationException("Environment does not belong to the specified project.");

        environment.ProjectEnvironmentVariables.ApplyVariableChanges(command.Variables);

        await projectEnvironmentRepository.UpdateAsync(environment, cancellationToken);

        return new UpsertEnvironmentVariablesResponse();
    }
}
