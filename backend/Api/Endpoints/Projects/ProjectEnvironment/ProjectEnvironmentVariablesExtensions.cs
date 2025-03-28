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
            if (current.Any(x => x.GetHash() == variable.GetHash()))
                continue;

            var existingVariable = current
                .FirstOrDefault(x => x.Key == variable.Key && x.SystemType == variable.SystemType);

            if (existingVariable != null)
                current.Remove(existingVariable);

            // Add new variable
            current.Add(new ProjectEnvironmentVariable
            {
                SystemType = variable.SystemType,
                Key = variable.Key,
                Value = variable.Value
            });
        }
    }
}