using System.Linq.Expressions;
using Api.Abstractions;
using Api.Data;
using Api.Data.Repositories;
using Api.Hosting.Endpoints.Projects.Services;
using Auth.Abstractions;
using NSubstitute;

namespace Api.Tests.Endpoints.Projects.Services;

public class ReportEnvironmentServicesCommandHandlerTests
{
    private readonly IProjectServiceRepository _projectServiceRepository;
    private readonly IProjectRepository _projectRepository;
    private readonly IDomainRepository _domainRepository;
    private readonly ReportEnvironmentServicesCommandHandler _handler;

    public ReportEnvironmentServicesCommandHandlerTests()
    {
        _projectServiceRepository = Substitute.For<IProjectServiceRepository>();
        _projectRepository = Substitute.For<IProjectRepository>();
        _domainRepository = Substitute.For<IDomainRepository>();
        _handler = new ReportEnvironmentServicesCommandHandler(
            _projectServiceRepository,
            _projectRepository,
            _domainRepository);
    }

    [Fact]
    public async Task Handle_WhenReportedServiceMatchesSoftDeletedRow_ResurrectsIt()
    {
        var projectId = ProjectId.CreateNew();
        var environmentId = ProjectEnvironmentId.CreateNew();

        var softDeletedBackend = new ProjectService
        {
            Id = ProjectServiceId.CreateNew(),
            ProjectId = projectId,
            ProjectEnvironmentId = environmentId,
            Name = "Backend",
            Port = "1235",
            Protocol = Protocol.Http,
            IsSystem = true,
            DeletedAtUtc = DateTime.UtcNow.AddDays(-4)
        };

        var project = BuildProject(projectId, softDeletedBackend);
        StubGetSingle(project);

        var command = new ReportEnvironmentServicesCommand(
            projectId,
            environmentId,
            [new ServicePortInfo("Backend", "1235", Protocol.Http)]);

        await _handler.Handle(command, CancellationToken.None);

        Assert.Null(softDeletedBackend.DeletedAtUtc);
        await _projectServiceRepository
            .Received(1)
            .UpdateAsync(softDeletedBackend, Arg.Any<CancellationToken>());
        await _projectServiceRepository
            .DidNotReceive()
            .DeleteAsync(Arg.Any<ProjectServiceId>(), Arg.Any<CancellationToken>());
        await _projectServiceRepository
            .DidNotReceive()
            .AddAsync(Arg.Any<ProjectService>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenExistingServicePortChanges_SyncsMatchingDomainPort()
    {
        // Repro for the fusee.app 502: deploy swaps container port from 1234→80,
        // hosting reports App@80, handler updates ProjectService but leaves
        // Domain.Port at 1234, Traefik routes to :1234 and every request 502s.
        var projectId = ProjectId.CreateNew();
        var environmentId = ProjectEnvironmentId.CreateNew();

        var appService = new ProjectService
        {
            Id = ProjectServiceId.CreateNew(),
            ProjectId = projectId,
            ProjectEnvironmentId = environmentId,
            Name = "App",
            Port = "1234",
            Protocol = Protocol.Http,
            IsSystem = true
        };

        var internalDomain = new InternalDomain
        {
            Id = DomainId.CreateNew(),
            ProjectId = projectId,
            ProjectEnvironmentId = environmentId,
            ServiceName = "App",
            Port = "1234",
            Protocol = Protocol.Http,
            Subdomain = "fusee"
        };
        var externalDomain = new ExternalDomain
        {
            Id = DomainId.CreateNew(),
            ProjectId = projectId,
            ProjectEnvironmentId = environmentId,
            ServiceName = "App",
            Port = "1234",
            Protocol = Protocol.Http,
            Value = "fusee.app",
            IsVerified = true
        };
        // An unrelated domain that should NOT get touched.
        var unrelatedDomain = new InternalDomain
        {
            Id = DomainId.CreateNew(),
            ProjectId = projectId,
            ProjectEnvironmentId = environmentId,
            ServiceName = "OtherService",
            Port = "5000",
            Protocol = Protocol.Http,
            Subdomain = "other"
        };

        var project = BuildProject(projectId, new[] { appService },
            new Domain[] { internalDomain, externalDomain, unrelatedDomain });
        StubGetSingle(project);

        var command = new ReportEnvironmentServicesCommand(
            projectId,
            environmentId,
            [new ServicePortInfo("App", "80", Protocol.Http)]);

        await _handler.Handle(command, CancellationToken.None);

        Assert.Equal("80", appService.Port);
        Assert.Equal("80", internalDomain.Port);
        Assert.Equal("80", externalDomain.Port);
        Assert.Equal("5000", unrelatedDomain.Port);
    }

    [Fact]
    public async Task Handle_WhenServiceProtocolChanges_SyncsMatchingDomainProtocol()
    {
        var projectId = ProjectId.CreateNew();
        var environmentId = ProjectEnvironmentId.CreateNew();

        var service = new ProjectService
        {
            Id = ProjectServiceId.CreateNew(),
            ProjectId = projectId,
            ProjectEnvironmentId = environmentId,
            Name = "Db",
            Port = "5432",
            Protocol = Protocol.Http,
            IsSystem = false
        };
        var domain = new InternalDomain
        {
            Id = DomainId.CreateNew(),
            ProjectId = projectId,
            ProjectEnvironmentId = environmentId,
            ServiceName = "Db",
            Port = "5432",
            Protocol = Protocol.Http,
            Subdomain = "db"
        };

        var project = BuildProject(projectId, new[] { service }, new Domain[] { domain });
        StubGetSingle(project);

        var command = new ReportEnvironmentServicesCommand(
            projectId,
            environmentId,
            [new ServicePortInfo("Db", "5432", Protocol.Tcp)]);

        await _handler.Handle(command, CancellationToken.None);

        Assert.Equal(Protocol.Tcp, service.Protocol);
        Assert.Equal(Protocol.Tcp, domain.Protocol);
    }

    [Fact]
    public async Task Handle_DoesNotSyncDomainsFromOtherEnvironments()
    {
        var projectId = ProjectId.CreateNew();
        var environmentId = ProjectEnvironmentId.CreateNew();
        var otherEnvironmentId = ProjectEnvironmentId.CreateNew();

        var service = new ProjectService
        {
            Id = ProjectServiceId.CreateNew(),
            ProjectId = projectId,
            ProjectEnvironmentId = environmentId,
            Name = "App",
            Port = "1234",
            Protocol = Protocol.Http,
            IsSystem = true
        };
        var otherEnvDomain = new InternalDomain
        {
            Id = DomainId.CreateNew(),
            ProjectId = projectId,
            ProjectEnvironmentId = otherEnvironmentId,
            ServiceName = "App",
            Port = "1234",
            Protocol = Protocol.Http,
            Subdomain = "other-env"
        };

        var project = BuildProject(projectId, new[] { service }, new Domain[] { otherEnvDomain });
        StubGetSingle(project);

        var command = new ReportEnvironmentServicesCommand(
            projectId,
            environmentId,
            [new ServicePortInfo("App", "80", Protocol.Http)]);

        await _handler.Handle(command, CancellationToken.None);

        // Cross-env domain must be left alone — a different env's handler call
        // owns it.
        Assert.Equal("1234", otherEnvDomain.Port);
    }

    private Project BuildProject(ProjectId projectId, params ProjectService[] services)
    {
        return BuildProject(projectId, services, Array.Empty<Domain>());
    }

    private Project BuildProject(ProjectId projectId, ProjectService[] services, Domain[] domains)
    {
        return new Project
        {
            Id = projectId,
            Name = "test",
            RepoUri = new Uri("https://github.com/test/repo"),
            UserId = UserId.CreateNew(),
            LocationId = LocationId.CreateNew(),
            State = ProjectState.Running,
            ServerTierId = new ServerTierId("T0"),
            ProjectServices = services.ToList(),
            Domains = domains.ToList()
        };
    }

    private void StubGetSingle(Project project)
    {
        _projectRepository
            .GetSingleAsync(
                Arg.Any<Expression<Func<Project, bool>>>(),
                Arg.Any<Func<IQueryable<Project>, IQueryable<Project>>>(),
                Arg.Any<CancellationToken>())
            .Returns(project);
    }
}
