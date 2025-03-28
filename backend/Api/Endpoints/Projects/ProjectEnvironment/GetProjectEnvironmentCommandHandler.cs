using Api.Abstractions;
using Api.Data;
using Api.Data.Repositories;
using Core.MediatR;

namespace Api.Endpoints.Projects.ProjectEnvironment;

public sealed record GetProjectEnvironmentCommandHandler(
    IProjectRepository ProjectRepository,
    IDefaultEnvironmentVariablesAppender VariablesAppender
) : ICommandHandler<GetProjectEnvironmentCommand, GetProjectEnvironmentResponse>
{
    public async Task<GetProjectEnvironmentResponse> Handle(
        GetProjectEnvironmentCommand command,
        CancellationToken cancellationToken = default)
    {

        var projectIdConverted = ProjectId.Create(command.ProjectId);

        var environment = await ProjectRepository.GetProjectEnvironmentByBranchAsync(
            projectIdConverted,
            command.Branch);

        var environmentVariables = environment?.ProjectEnvironmentVariables?.Select(
            x => new ProjectEnvironmentVariableDto(
                x.SystemType,
                x.Key,
                x.Value)).ToList() ?? [];

        environmentVariables = await VariablesAppender.AppendDefaultVariablesAsync(
            environmentVariables,
            projectIdConverted);

        return new(environmentVariables);
    }
}
