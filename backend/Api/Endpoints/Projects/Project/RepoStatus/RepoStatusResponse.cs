namespace Api.Endpoints.Projects.Project.RepoStatus;

public enum RepoStatus
{
    Ok,
    Renamed,
    Transferred,
    Archived,
    NotFound,
    Error,
}

public sealed record RepoStatusResponse(
    RepoStatus Status,
    string? CurrentName = null,
    string? NewName = null,
    string? NewUrl = null,
    bool? IsPrivate = null,
    bool? IsArchived = null,
    string? DefaultBranch = null,
    string? Message = null);
