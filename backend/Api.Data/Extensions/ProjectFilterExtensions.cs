namespace Api.Data.Extensions;

public static class ProjectFilterExtensions
{
    public static IEnumerable<Project> FilterByStateAndSearch(
        this IEnumerable<Project> projects,
        ProjectStateFilter stateFilter = ProjectStateFilter.All,
        string? search = null)
    {
        var filtered = projects.Where(p => (stateFilter & ToFilter(p.State)) != 0);
        if (!string.IsNullOrWhiteSpace(search))
        {
            filtered = filtered.Where(p => p.Name.Contains(search, System.StringComparison.OrdinalIgnoreCase));
        }
        return filtered;
    }

    private static ProjectStateFilter ToFilter(ProjectState state)
    {
        return state switch
        {
            ProjectState.Unknown => ProjectStateFilter.Unknown,
            ProjectState.Running => ProjectStateFilter.Running,
            ProjectState.Starting => ProjectStateFilter.Starting,
            ProjectState.Stopped => ProjectStateFilter.Stopped,
            ProjectState.Deleting => ProjectStateFilter.Deleting,
            _ => ProjectStateFilter.None
        };
    }
}
