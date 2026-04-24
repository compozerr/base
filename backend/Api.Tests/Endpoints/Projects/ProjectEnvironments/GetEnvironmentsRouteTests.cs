using Api.Abstractions;
using Api.Data;
using Api.Data.Repositories;
using Api.Hosting.Endpoints.Projects.ProjectEnvironments;
using Microsoft.AspNetCore.Http.HttpResults;
using NSubstitute;

namespace Api.Tests.Endpoints.Projects.ProjectEnvironments;

public class GetEnvironmentsRouteTests
{
    private readonly IProjectEnvironmentRepository _envRepository;

    public GetEnvironmentsRouteTests()
    {
        _envRepository = Substitute.For<IProjectEnvironmentRepository>();
    }

    [Fact]
    public async Task ExecuteAsync_ReturnsOneDtoPerEnv_PreservingIdNameTypeAndSubdomain()
    {
        var projectId = ProjectId.CreateNew();
        var prodEnvId = ProjectEnvironmentId.CreateNew();
        var stagingEnvId = ProjectEnvironmentId.CreateNew();

        var prod = new ProjectEnvironment
        {
            Id = prodEnvId,
            ProjectId = projectId,
            Branches = new List<string> { "main" },
            AutoDeploy = true,
            Type = EnvironmentType.Production,
        };
        var staging = new ProjectEnvironment
        {
            Id = stagingEnvId,
            ProjectId = projectId,
            Branches = new List<string> { "main" },
            AutoDeploy = true,
            Type = EnvironmentType.Preview,
            Name = "staging",
            Subdomain = "staging-routr-backend",
        };

        _envRepository
            .GetByProjectIdAsync(projectId, Arg.Any<CancellationToken>())
            .Returns(new List<ProjectEnvironment> { prod, staging });

        var result = await GetEnvironmentsRoute.ExecuteAsync(projectId, _envRepository, CancellationToken.None);

        Assert.Equal(2, result.Value!.Count);

        var prodDto = Assert.Single(result.Value, d => d.Id == prodEnvId);
        Assert.Equal(EnvironmentType.Production, prodDto.Type);
        Assert.Null(prodDto.Subdomain);

        var stagingDto = Assert.Single(result.Value, d => d.Id == stagingEnvId);
        Assert.Equal(EnvironmentType.Preview, stagingDto.Type);
        Assert.Equal("staging", stagingDto.Name);
        Assert.Equal("staging-routr-backend", stagingDto.Subdomain);
    }

    [Fact]
    public async Task ExecuteAsync_ReturnsEmptyListWhenProjectHasNoEnvs()
    {
        var projectId = ProjectId.CreateNew();
        _envRepository
            .GetByProjectIdAsync(projectId, Arg.Any<CancellationToken>())
            .Returns(new List<ProjectEnvironment>());

        var result = await GetEnvironmentsRoute.ExecuteAsync(projectId, _envRepository, CancellationToken.None);

        Assert.Empty(result.Value!);
    }
}
