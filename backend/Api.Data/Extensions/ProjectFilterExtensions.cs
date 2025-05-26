namespace Api.Data.Extensions;

public static class ProjectFilterExtensions
{
    public static List<ProjectState> ToStates(this ProjectStateFilter filter)
    {
        var states = new List<ProjectState>();
        if ((filter & ProjectStateFilter.Unknown) != 0) states.Add(ProjectState.Unknown);
        if ((filter & ProjectStateFilter.Running) != 0) states.Add(ProjectState.Running);
        if ((filter & ProjectStateFilter.Starting) != 0) states.Add(ProjectState.Starting);
        if ((filter & ProjectStateFilter.Stopped) != 0) states.Add(ProjectState.Stopped);
        if ((filter & ProjectStateFilter.Deleting) != 0) states.Add(ProjectState.Deleting);
        return states;
    }
}
