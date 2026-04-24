using Api.Abstractions;
using Api.Data;
using Api.Data.ProjectTypes;
using Api.Data.Repositories;
using Core.MediatR;

namespace Api.Endpoints.Projects.Environments.Variables;

public sealed class GetEnvironmentVariablesCommandHandler(
    IProjectEnvironmentRepository projectEnvironmentRepository,
    IProjectRepository projectRepository,
    IDefaultEnvironmentVariablesAppender variablesAppender,
    IProjectTypeConfigurationFactory configurationFactory)
    : ICommandHandler<GetEnvironmentVariablesCommand, GetProjectEnvironmentResponse?>
{
    public async Task<GetProjectEnvironmentResponse?> Handle(
        GetEnvironmentVariablesCommand command,
        CancellationToken cancellationToken = default)
    {
        var environment = await projectEnvironmentRepository.GetByIdWithVariablesAsync(command.EnvironmentId);

        if (environment is null) return null;
        if (environment.ProjectId != command.ProjectId) return null;

        var project = await projectRepository.GetByIdAsync(command.ProjectId, cancellationToken);

        var environmentVariables = environment.ProjectEnvironmentVariables?
            .Select(x => new ProjectEnvironmentVariableDto(x.SystemType, x.Key, x.Value, false))
            .ToList() ?? [];

        environmentVariables = await variablesAppender.AppendDefaultVariablesAsync(
            environmentVariables,
            command.ProjectId);

        var projectType = project?.Type ?? ProjectType.Compozerr;
        var configuration = configurationFactory.GetConfiguration(projectType);

        return new GetProjectEnvironmentResponse(
            environment.AutoDeploy,
            projectType,
            configuration.ToDto(),
            environmentVariables);
    }
}
