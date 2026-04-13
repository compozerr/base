using Api.Abstractions;
using Api.Data;
using Api.Data.Repositories;
using Api.Hosting.Endpoints.Projects.Config;
using Auth.Abstractions;
using NSubstitute;

namespace Api.Tests.Endpoints.Projects.Config;

public class ReportProjectConfigCommandHandlerTests
{
    private readonly IProjectRepository _projectRepository;
    private readonly ReportProjectConfigCommandHandler _handler;

    public ReportProjectConfigCommandHandlerTests()
    {
        _projectRepository = Substitute.For<IProjectRepository>();
        _handler = new ReportProjectConfigCommandHandler(_projectRepository);
    }

    [Fact]
    public async Task Handle_WhenProjectNotFound_ReturnsResponseWithoutUpdate()
    {
        var projectId = ProjectId.CreateNew();
        var command = new ReportProjectConfigCommand(projectId, "new-name");

        _projectRepository.GetByIdAsync(projectId, Arg.Any<CancellationToken>())
            .Returns((Project?)null);

        var result = await _handler.Handle(command);

        Assert.NotNull(result);
        await _projectRepository.DidNotReceive().UpdateAsync(Arg.Any<Project>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenNameChanged_UpdatesProject()
    {
        var projectId = ProjectId.CreateNew();
        var project = new Project
        {
            Id = projectId,
            Name = "old-name",
            RepoUri = new Uri("https://github.com/test/repo"),
            UserId = UserId.CreateNew(),
            LocationId = LocationId.CreateNew(),
            State = ProjectState.Running,
            ServerTierId = new ServerTierId("T0")
        };
        var command = new ReportProjectConfigCommand(projectId, "new-name");

        _projectRepository.GetByIdAsync(projectId, Arg.Any<CancellationToken>())
            .Returns(project);

        var result = await _handler.Handle(command);

        Assert.NotNull(result);
        Assert.Equal("new-name", project.Name);
        await _projectRepository.Received(1).UpdateAsync(project, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenNameUnchanged_DoesNotUpdate()
    {
        var projectId = ProjectId.CreateNew();
        var project = new Project
        {
            Id = projectId,
            Name = "same-name",
            RepoUri = new Uri("https://github.com/test/repo"),
            UserId = UserId.CreateNew(),
            LocationId = LocationId.CreateNew(),
            State = ProjectState.Running,
            ServerTierId = new ServerTierId("T0")
        };
        var command = new ReportProjectConfigCommand(projectId, "same-name");

        _projectRepository.GetByIdAsync(projectId, Arg.Any<CancellationToken>())
            .Returns(project);

        var result = await _handler.Handle(command);

        Assert.NotNull(result);
        Assert.Equal("same-name", project.Name);
        await _projectRepository.DidNotReceive().UpdateAsync(Arg.Any<Project>(), Arg.Any<CancellationToken>());
    }
}
