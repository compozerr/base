using Api.Abstractions;
using Api.Data;
using Api.Data.ProjectTypes;
using Api.Data.Repositories;
using Api.Hosting.Endpoints.Projects.EnvironmentVariables;
using Auth.Abstractions;
using NSubstitute;

namespace Api.Tests.Endpoints.Projects.EnvironmentVariables;

public class GetEnvironmentVariablesByIdCommandHandlerTests
{
    private readonly IProjectEnvironmentRepository _environmentRepository = Substitute.For<IProjectEnvironmentRepository>();
    private readonly IProjectRepository _projectRepository = Substitute.For<IProjectRepository>();
    private readonly IDefaultEnvironmentVariablesAppender _variablesAppender = Substitute.For<IDefaultEnvironmentVariablesAppender>();
    private readonly IProjectTypeConfigurationFactory _configurationFactory = Substitute.For<IProjectTypeConfigurationFactory>();

    private GetEnvironmentVariablesByIdCommandHandler Handler() =>
        new(_environmentRepository, _projectRepository, _variablesAppender, _configurationFactory);

    [Fact]
    public async Task Handle_WhenEnvironmentExists_ReturnsVariablesAndConfig()
    {
        var projectId = ProjectId.CreateNew();
        var environmentId = ProjectEnvironmentId.CreateNew();

        var env = new ProjectEnvironment
        {
            Id = environmentId,
            ProjectId = projectId,
            AutoDeploy = true,
            Branches = ["main"],
            ProjectEnvironmentVariables = new List<ProjectEnvironmentVariable>
            {
                new() { Key = "FOO", Value = "bar", SystemType = SystemType.Backend }
            }
        };
        _environmentRepository.GetByIdWithVariablesAsync(environmentId).Returns(env);

        var project = new Project
        {
            Id = projectId,
            Type = ProjectType.Compozerr,
            Name = "test-project",
            RepoUri = new Uri("https://github.com/test/repo"),
            UserId = new UserId(Guid.NewGuid()),
            LocationId = new LocationId(Guid.NewGuid()),
            State = ProjectState.Running,
            ServerTierId = new ServerTierId("basic"),
        };
        _projectRepository.GetByIdAsync(projectId, Arg.Any<CancellationToken>()).Returns(project);

        var config = Substitute.For<IProjectTypeConfiguration>();
        config.AllowedSystemTypes.Returns([]);
        config.EnvironmentFiles.Returns([]);
        config.DefaultSystemType.Returns("backend");
        config.StartCommand.Returns("start");
        config.DefaultServices.Returns([]);
        _configurationFactory.GetConfiguration(ProjectType.Compozerr).Returns(config);

        _variablesAppender
            .AppendDefaultVariablesAsync(Arg.Any<List<ProjectEnvironmentVariableDto>>(), projectId)
            .Returns(ci => ci.Arg<List<ProjectEnvironmentVariableDto>>());

        var response = await Handler().Handle(
            new GetEnvironmentVariablesByIdCommand(projectId, environmentId),
            CancellationToken.None);

        Assert.True(response!.AutoDeploy);
        Assert.Equal(ProjectType.Compozerr, response.ProjectType);
        Assert.Single(response.Variables);
        Assert.Equal("FOO", response.Variables[0].Key);
        Assert.Equal("bar", response.Variables[0].Value);
        Assert.NotNull(response.Configuration);
    }

    [Fact]
    public async Task Handle_WhenEnvironmentNotFound_ReturnsNull()
    {
        var projectId = ProjectId.CreateNew();
        var environmentId = ProjectEnvironmentId.CreateNew();

        _environmentRepository.GetByIdWithVariablesAsync(environmentId).Returns((ProjectEnvironment?)null);

        var response = await Handler().Handle(
            new GetEnvironmentVariablesByIdCommand(projectId, environmentId),
            CancellationToken.None);

        Assert.Null(response);
    }

    [Fact]
    public async Task Handle_WhenEnvironmentBelongsToDifferentProject_ReturnsNull()
    {
        var requestedProjectId = ProjectId.CreateNew();
        var otherProjectId = ProjectId.CreateNew();
        var environmentId = ProjectEnvironmentId.CreateNew();

        var env = new ProjectEnvironment
        {
            Id = environmentId,
            ProjectId = otherProjectId,
            AutoDeploy = true,
            Branches = ["main"],
            ProjectEnvironmentVariables = []
        };
        _environmentRepository.GetByIdWithVariablesAsync(environmentId).Returns(env);

        var response = await Handler().Handle(
            new GetEnvironmentVariablesByIdCommand(requestedProjectId, environmentId),
            CancellationToken.None);

        Assert.Null(response);
    }
}
