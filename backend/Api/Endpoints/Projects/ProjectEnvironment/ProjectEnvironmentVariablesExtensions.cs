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
}