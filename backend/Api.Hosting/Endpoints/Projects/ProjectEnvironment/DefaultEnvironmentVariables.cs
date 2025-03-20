using Api.Abstractions;
using Api.Data;
using Api.Data.Repositories;

namespace Api.Hosting.Endpoints.Projects.ProjectEnvironment;

public interface IDefaultEnvironmentVariablesAppender
{
    Task<List<ProjectEnvironmentVariableDto>> AppendDefaultVariablesAsync(
        List<ProjectEnvironmentVariableDto> current,
        ProjectId projectId);
}

public sealed class DefaultEnvironmentVariablesAppender(ProjectRepository projectRepository) : IDefaultEnvironmentVariablesAppender
{
    public async Task<List<ProjectEnvironmentVariableDto>> AppendDefaultVariablesAsync(
        List<ProjectEnvironmentVariableDto> current,
        ProjectId projectId)
    {
        var project = await projectRepository.GetProjectByIdWithDomainsAsync(projectId);

        var frontendUrl = ((InternalDomain?)project?.Domains?.FirstOrDefault(x => x.Type == DomainType.Internal && x.ServiceName.Equals("Frontend", StringComparison.InvariantCultureIgnoreCase)))?.Value;
        var backendUrl = ((InternalDomain?)project?.Domains?.FirstOrDefault(x => x.Type == DomainType.Internal && x.ServiceName.Equals("Backend", StringComparison.InvariantCultureIgnoreCase)))?.Value;

        if (frontendUrl is { })
        {
            current.AddIfNotFound(new(SystemType.Backend, "FRONTEND_URL", frontendUrl));
        }

        if (backendUrl is { })
        {
            current.AddIfNotFound(new(SystemType.Frontend, "VITE_BACKEND_URL", backendUrl));
        }

        return current;
    }
}
