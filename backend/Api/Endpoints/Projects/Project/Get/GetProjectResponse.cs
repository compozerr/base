using Api.Data;

namespace Api.Endpoints.Projects.Project.Get;

public sealed record GetProjectResponse(
    Guid Id,
    string Name,
    string RepoName,
    ProjectState State,
    DateTime StartDate,
    List<string> Domains,
    string? PrimaryDomain);
