using Api.Abstractions;
using Api.Data;
using Api.Data.Repositories;
using Api.Hosting.Endpoints.Projects.ProjectDomains;
using Auth.Abstractions;
using Microsoft.AspNetCore.Http.HttpResults;
using NSubstitute;

namespace Api.Tests.Endpoints.Projects.ProjectDomains;

public class GetDomainsRouteTests
{
    private readonly IProjectRepository _projectRepository;

    public GetDomainsRouteTests()
    {
        _projectRepository = Substitute.For<IProjectRepository>();
    }

    [Fact]
    public async Task ExecuteAsync_IncludesProjectEnvironmentIdOnEachReturnedDomain()
    {
        // Regression: hosting/manager needs projectEnvironmentId to route each
        // custom domain to the right env's VM. Without it, every non-preview
        // domain gets swept onto the production router (kurvo bug — mobile
        // hits `test-alert.kurvo.app` which should be staging but landed on
        // prod because the response didn't expose env membership).
        var projectId = ProjectId.CreateNew();
        var prodEnv = ProjectEnvironmentId.CreateNew();
        var stagingEnv = ProjectEnvironmentId.CreateNew();

        var prodDomain = new ExternalDomain
        {
            Id = DomainId.CreateNew(),
            ProjectId = projectId,
            ProjectEnvironmentId = prodEnv,
            ServiceName = "Heimdall",
            Port = "3456",
            Protocol = Protocol.Http,
            Value = "alert.kurvo.app",
            IsVerified = true,
        };
        var stagingDomain = new ExternalDomain
        {
            Id = DomainId.CreateNew(),
            ProjectId = projectId,
            ProjectEnvironmentId = stagingEnv,
            ServiceName = "Heimdall",
            Port = "3456",
            Protocol = Protocol.Http,
            Value = "test-alert.kurvo.app",
            IsVerified = true,
        };

        var project = new Project
        {
            Id = projectId,
            Name = "kurvo-backend",
            RepoUri = new Uri("https://github.com/test/kurvo"),
            UserId = UserId.CreateNew(),
            LocationId = LocationId.CreateNew(),
            State = ProjectState.Running,
            ServerTierId = new ServerTierId("T0"),
            Domains = new List<Domain> { prodDomain, stagingDomain },
        };

        _projectRepository.GetProjectByIdWithDomainsAsync(projectId).Returns(project);

        var result = await GetDomainsRoute.ExecuteAsync(projectId, _projectRepository);

        var ok = Assert.IsType<Ok<List<DomainDto>>>(result.Result);
        Assert.NotNull(ok.Value);
        Assert.Equal(2, ok.Value!.Count);

        var prodDto = Assert.Single(ok.Value, d => d.Value == "alert.kurvo.app");
        var stagingDto = Assert.Single(ok.Value, d => d.Value == "test-alert.kurvo.app");
        Assert.Equal(prodEnv, prodDto.ProjectEnvironmentId);
        Assert.Equal(stagingEnv, stagingDto.ProjectEnvironmentId);
    }

    [Fact]
    public async Task ExecuteAsync_LeavesProjectEnvironmentIdNullWhenDomainIsUnpinned()
    {
        // Defensive: Domain.ProjectEnvironmentId is nullable in the schema,
        // so the DTO has to carry that null through to the manager unchanged.
        var projectId = ProjectId.CreateNew();
        var unpinnedDomain = new ExternalDomain
        {
            Id = DomainId.CreateNew(),
            ProjectId = projectId,
            ProjectEnvironmentId = null,
            ServiceName = "Web",
            Port = "8080",
            Protocol = Protocol.Http,
            Value = "legacy.kurvo.app",
            IsVerified = true,
        };

        var project = new Project
        {
            Id = projectId,
            Name = "kurvo-backend",
            RepoUri = new Uri("https://github.com/test/kurvo"),
            UserId = UserId.CreateNew(),
            LocationId = LocationId.CreateNew(),
            State = ProjectState.Running,
            ServerTierId = new ServerTierId("T0"),
            Domains = new List<Domain> { unpinnedDomain },
        };

        _projectRepository.GetProjectByIdWithDomainsAsync(projectId).Returns(project);

        var result = await GetDomainsRoute.ExecuteAsync(projectId, _projectRepository);

        var ok = Assert.IsType<Ok<List<DomainDto>>>(result.Result);
        Assert.NotNull(ok.Value);
        var dto = Assert.Single(ok.Value!);
        Assert.Null(dto.ProjectEnvironmentId);
    }
}
