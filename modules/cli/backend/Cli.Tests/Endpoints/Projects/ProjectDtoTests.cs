using Api.Abstractions;
using Api.Data;
using Auth.Abstractions;
using Cli.Endpoints.Projects;
using FluentAssertions;

namespace Cli.Tests.Endpoints.Projects;

/// <summary>
/// F-P2-3: <see cref="ProjectDto.ServerId"/> is a deprecated wire-compat field.
/// It must resolve through prod-env ServerId → project legacy ServerId hint, and
/// fall back to <c>null</c> (not throw) when nothing is allocated yet. The prior
/// code threw <see cref="InvalidOperationException"/> in exactly the transition
/// cases (zero envs, or a prod env whose ServerId hasn't been backfilled) that
/// the project-level hint exists to cover, breaking old-CLI downgrade safety.
/// </summary>
public class ProjectDtoTests
{
    private static Project NewProject(ServerId? projectHint, List<ProjectEnvironment>? envs)
        => new()
        {
            Id = ProjectId.CreateNew(),
            Name = "p",
            RepoUri = new Uri("https://github.com/test/repo"),
            UserId = UserId.CreateNew(),
            LocationId = LocationId.CreateNew(),
            State = ProjectState.Running,
            ServerTierId = new ServerTierId("T0"),
            ServerId = projectHint,
            ProjectEnvironments = envs,
        };

    private static ProjectEnvironment NewEnv(EnvironmentType type, ServerId? serverId)
        => new()
        {
            Id = ProjectEnvironmentId.CreateNew(),
            ProjectId = ProjectId.CreateNew(),
            Branches = new List<string>(),
            AutoDeploy = true,
            Type = type,
            ServerId = serverId,
        };

    [Fact]
    public void FromProject_FallsBackToProjectHint_WhenProjectHasNoEnvs()
    {
        var hint = ServerId.CreateNew();
        var project = NewProject(hint, envs: null);

        var dto = ProjectDto.FromProject(project, productionEnvServerId: null);

        dto.ServerId.Should().Be(hint.Value);
    }

    [Fact]
    public void FromProject_FallsBackToProjectHint_WhenProdEnvServerIdIsNull()
    {
        var hint = ServerId.CreateNew();
        var prodEnv = NewEnv(EnvironmentType.Production, serverId: null);
        var project = NewProject(hint, envs: new List<ProjectEnvironment> { prodEnv });

        var dto = ProjectDto.FromProject(project, productionEnvServerId: null);

        dto.ServerId.Should().Be(hint.Value);
    }

    [Fact]
    public void FromProject_UsesProdEnvServerId_WhenAllocated()
    {
        var envServerId = ServerId.CreateNew();
        var prodEnv = NewEnv(EnvironmentType.Production, serverId: envServerId);
        // Project hint differs — must NOT win over the prod env's own ServerId.
        var project = NewProject(ServerId.CreateNew(), envs: new List<ProjectEnvironment> { prodEnv });

        var dto = ProjectDto.FromProject(project, productionEnvServerId: null);

        dto.ServerId.Should().Be(envServerId.Value);
    }

    [Fact]
    public void FromProject_ReturnsNull_WhenNothingAllocated()
    {
        // No envs, no project hint, no production-env hint — the deprecated field
        // is nullable, so this must produce null rather than throwing.
        var project = NewProject(projectHint: null, envs: null);

        var dto = ProjectDto.FromProject(project, productionEnvServerId: null);

        dto.ServerId.Should().BeNull();
    }
}
