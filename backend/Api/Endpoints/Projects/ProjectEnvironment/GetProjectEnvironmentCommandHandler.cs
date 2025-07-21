using Api.Abstractions;
using Api.Data;
using Api.Data.Repositories;
using Core.MediatR;

namespace Api.Endpoints.Projects.ProjectEnvironment;

public class GetProjectEnvironmentCommandHandler(
    IProjectEnvironmentRepository projectEnvironmentRepository,
    IDefaultEnvironmentVariablesAppender variablesAppender
) : ICommandHandler<GetProjectEnvironmentCommand, GetProjectEnvironmentResponse>
{
    public async Task<GetProjectEnvironmentResponse> Handle(
        GetProjectEnvironmentCommand command,
        CancellationToken cancellationToken = default)
    {
        var environment = await projectEnvironmentRepository.GetProjectEnvironmentByBranchAsync(
            command.ProjectId,
            command.Branch);

        var environmentVariables = environment?.ProjectEnvironmentVariables?.Select(
            x => new ProjectEnvironmentVariableDto(
                x.SystemType,
                x.Key,
                x.Value,
                false)).ToList() ?? [];

        environmentVariables = await variablesAppender.AppendDefaultVariablesAsync(
            environmentVariables,
            command.ProjectId);

        return new(environment?.AutoDeploy ?? false, environmentVariables);
    }
}
