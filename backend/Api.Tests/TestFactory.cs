using Api.Abstractions;
using Api.Data;
using Auth.Abstractions;

namespace Api.Tests;

internal static class TestFactory
{
    public static Project Project(
        ProjectId projectId,
        string name = "test",
        List<Domain>? domains = null,
        List<ProjectService>? projectServices = null)
        => new()
        {
            Id = projectId,
            Name = name,
            RepoUri = new Uri("https://github.com/test/repo"),
            UserId = UserId.CreateNew(),
            LocationId = LocationId.CreateNew(),
            State = ProjectState.Running,
            ServerTierId = new ServerTierId("T0"),
            Domains = domains ?? new List<Domain>(),
            ProjectServices = projectServices ?? new List<ProjectService>(),
        };
}
