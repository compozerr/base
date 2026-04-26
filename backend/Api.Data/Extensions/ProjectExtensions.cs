namespace Api.Data.Extensions;

public static class ProjectExtensions
{
    /// <summary>
    /// Derives the display-only state shown in the UI. Returns <see cref="ProjectState.Unhealthy"/>
    /// when the project is <see cref="ProjectState.Running"/> but any non-preview environment
    /// has <see cref="EnvironmentHealthStatus.Unhealthy"/>. Otherwise returns the stored
    /// <see cref="Project.State"/>.
    /// </summary>
    public static ProjectState GetDisplayState(this Project project)
    {
        if (project.State != ProjectState.Running) return project.State;
        if (project.ProjectEnvironments is null) return project.State;

        var hasUnhealthyNonPreviewEnv = project.ProjectEnvironments.Any(e =>
            e.Type != EnvironmentType.Preview &&
            e.HealthStatus == EnvironmentHealthStatus.Unhealthy);

        return hasUnhealthyNonPreviewEnv ? ProjectState.Unhealthy : project.State;
    }
}
