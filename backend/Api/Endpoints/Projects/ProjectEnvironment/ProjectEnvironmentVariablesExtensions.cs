using Api.Data;

namespace Api.Endpoints.Projects.ProjectEnvironment;

public static class ProjectEnvironmentVariablesExtensions
{
    internal static void AddIfNotFound(this List<ProjectEnvironmentVariableDto> current, ProjectEnvironmentVariableDto variable)
    {
        if (current.Any(x => x.Key == variable.Key && x.SystemType == variable.SystemType))
        {
            return;
        }

        current.Add(variable);
    }

    internal static string GetHash(this ProjectEnvironmentVariableDto projectEnvironmentVariableDto) =>
            $"{projectEnvironmentVariableDto.SystemType}_{projectEnvironmentVariableDto.Key}_{projectEnvironmentVariableDto.Value}";
    internal static string GetHash(this ProjectEnvironmentVariable projectEnvironmentVariable) =>
        $"{projectEnvironmentVariable.SystemType}_{projectEnvironmentVariable.Key}_{projectEnvironmentVariable.Value}";

    internal static void ApplyVariableChanges(
        this ICollection<ProjectEnvironmentVariable>? current,
        List<ProjectEnvironmentVariableDto> newVariables)
    {
        current ??= [];

        foreach (var variable in newVariables)
        {
            var existingVariable = current.FirstOrDefault(x => x.GetHash() == variable.GetHash());

            if (existingVariable is { })
            {
                existingVariable.Value = variable.Value;
                continue;
            }

            var existingVariableWithSameKey = current
                .FirstOrDefault(x => x.Key == variable.Key && x.SystemType == variable.SystemType);

            if (existingVariableWithSameKey is { })
            {
                current.Remove(existingVariableWithSameKey);
            }

            current.Add(new ProjectEnvironmentVariable
            {
                SystemType = variable.SystemType,
                Key = variable.Key,
                Value = variable.Value
            });
        }
    }
}