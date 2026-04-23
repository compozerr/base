using Api.Abstractions;

namespace Api.Data;

public interface IDefaultEnvironmentVariablesAppender
{
    Task<List<ProjectEnvironmentVariableDto>> AppendDefaultVariablesAsync(
        List<ProjectEnvironmentVariableDto> current,
        ProjectId projectId);
}
