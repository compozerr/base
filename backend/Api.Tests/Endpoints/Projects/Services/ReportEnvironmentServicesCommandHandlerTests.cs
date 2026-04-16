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

    private Project BuildProject(ProjectId projectId, params ProjectService[] services)
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
            Domains = new List<Domain>()
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
