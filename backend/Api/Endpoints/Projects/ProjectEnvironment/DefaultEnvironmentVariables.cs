using Api.Abstractions;
using Api.Data;
using Api.Data.Repositories;

namespace Api.Endpoints.Projects.ProjectEnvironment;

public interface IDefaultEnvironmentVariablesAppender
{
    Task<List<ProjectEnvironmentVariableDto>> AppendDefaultVariablesAsync(
        List<ProjectEnvironmentVariableDto> current,
        ProjectId projectId);
}

public sealed class DefaultEnvironmentVariablesAppender(
    IProjectRepository projectRepository) : IDefaultEnvironmentVariablesAppender
{
    public async Task<List<ProjectEnvironmentVariableDto>> AppendDefaultVariablesAsync(
        List<ProjectEnvironmentVariableDto> current,
        ProjectId projectId)
    {
        var project = await projectRepository.GetProjectByIdWithDomainsAsync(projectId);

        var frontendInternalUrl = ((InternalDomain?)project?.Domains?.FirstOrDefault(x => x.Type == DomainType.Internal && x.ServiceName.Equals("Frontend", StringComparison.InvariantCultureIgnoreCase)))?.GetValue;
        var backendInternalUrl = ((InternalDomain?)project?.Domains?.FirstOrDefault(x => x.Type == DomainType.Internal && x.ServiceName.Equals("Backend", StringComparison.InvariantCultureIgnoreCase)))?.GetValue;
        var frontendExternalUrl = ((ExternalDomain?)project?.Domains?.FirstOrDefault(x => x.Type == DomainType.External && x.ServiceName.Equals("Frontend", StringComparison.InvariantCultureIgnoreCase)))?.GetValue;
        var backendExternalUrl = ((ExternalDomain?)project?.Domains?.FirstOrDefault(x => x.Type == DomainType.External && x.ServiceName.Equals("Backend", StringComparison.InvariantCultureIgnoreCase)))?.GetValue;

        var frontendUrl = frontendExternalUrl ?? frontendInternalUrl;
        var backendUrl = backendExternalUrl ?? backendInternalUrl;

        if (frontendUrl is { })
        {
            current.AddIfNotFound(new(SystemType.Backend, "FRONTEND_URL", $"https://{frontendUrl}", true));
            var corsOrigins = project?.Domains?
                .Where(x => x.ServiceName == "Frontend")
                .Select(x => $"https://{x.GetValue}")
                .Distinct()
                .ToList() ?? [];
            current.AddIfNotFound(new(SystemType.Backend, "CORS__ALLOWED_ORIGINS", string.Join(";", corsOrigins), true));
        }

        if (backendUrl is { })
        {
            current.AddIfNotFound(new(SystemType.Frontend, "VITE_BACKEND_URL", $"https://{backendUrl}", true));
        }

        current.AddIfNotFound(new(SystemType.Backend, "JWT__KEY", Guid.NewGuid().ToString(), true));
        current.AddIfNotFound(new(SystemType.Backend, "JWT__ISSUER", "compozerr", true));
        current.AddIfNotFound(new(SystemType.Backend, "JWT__AUDIENCE", "compozerr", true));

        return current;
    }
}
