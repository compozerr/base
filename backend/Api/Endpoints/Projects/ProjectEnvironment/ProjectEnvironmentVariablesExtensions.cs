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
        List<ProjectEnvironmentVariableDto> variables)
    {
        current ??= [];

        // Remove variables that are not in the new list
        var variablesToRemove = current
            .Where(x => !variables.Any(n => n.Key == x.Key && n.SystemType == x.SystemType))
            .ToList();
        
        foreach (var variable in variablesToRemove)
        {
            current.Remove(variable);
        }

        foreach (var variable in variables)
        {
            if (current.Any(x => x.GetHash() == variable.GetHash()))
                continue;

            var existingVariable = current
                .FirstOrDefault(x => x.Key == variable.Key && x.SystemType == x.SystemType);

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