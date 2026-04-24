using Api.Abstractions;
using Api.Data;
using Api.Data.Repositories;
using Api.Hosting.Endpoints.Projects.ProjectDomains;
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

        var project = TestFactory.Project(projectId, "kurvo-backend",
            domains: new List<Domain> { prodDomain, stagingDomain });

        _projectRepository.GetProjectByIdWithDomainsAsync(projectId).Returns(project);

        var result = await GetDomainsRoute.ExecuteAsync(projectId, _projectRepository);

        var ok = Assert.IsType<Ok<List<DomainDto>>>(result.Result);
        Assert.Equal(2, ok.Value!.Count);

        var prodDto = Assert.Single(ok.Value, d => d.Value == "alert.kurvo.app");
        var stagingDto = Assert.Single(ok.Value, d => d.Value == "test-alert.kurvo.app");
        Assert.Equal(prodEnv, prodDto.ProjectEnvironmentId);
        Assert.Equal(stagingEnv, stagingDto.ProjectEnvironmentId);
    }

    [Fact]
    public async Task ExecuteAsync_LeavesProjectEnvironmentIdNullWhenDomainIsUnpinned()
    {
        // Domain.ProjectEnvironmentId is nullable in the schema — manager
        // must still see `null` over the wire, not be dropped.
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

        var project = TestFactory.Project(projectId, "kurvo-backend",
            domains: new List<Domain> { unpinnedDomain });

        _projectRepository.GetProjectByIdWithDomainsAsync(projectId).Returns(project);

        var result = await GetDomainsRoute.ExecuteAsync(projectId, _projectRepository);

        var ok = Assert.IsType<Ok<List<DomainDto>>>(result.Result);
        var dto = Assert.Single(ok.Value!);
        Assert.Null(dto.ProjectEnvironmentId);
    }
}
