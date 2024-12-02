using System.Text.Json;
using Cli.Services;
using Cli.Services.Hosting;
using Cli.Services.Hosting.Providers.Flyio;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Moq;

namespace Cli.Tests.Features.Hosting.Providers.Flyio;

public class FlyioHostingProviderTests
{
    private readonly Mock<IFlyioNameGenerator> _nameGeneratorMock = new();
    private readonly Mock<IProcessService> _processServiceMock = new();
    private readonly Mock<IConfiguration> _configurationMock = new();


    [Fact]
    public async Task DeployAsync_should_generate_flyio_json()
    {
        _nameGeneratorMock.Setup(x => x.GenerateName(It.IsAny<string>(), It.IsAny<Platform>()))
            .Returns("somename");

        _configurationMock.Setup(x => x["FLYIO_ACCESS_TOKEN"]).Returns("token");

        _processServiceMock.Setup(x => x.RunProcessAsync(It.IsAny<string>()))
            .ReturnsAsync(new ProcessResponse(true, string.Empty));

        // Arrange
        var generator = new FlyioHostingProvider(_nameGeneratorMock.Object, _processServiceMock.Object, _configurationMock.Object);
        var deployRequest = new DeployRequest("my-app", "registry/path", Platform.Backend);

        var response = await generator.DeployAsync(deployRequest);

        Assert.NotNull(response);
        response.Success.Should().BeTrue();

        _nameGeneratorMock.Verify(x => x.GenerateName("my-app", Platform.Backend), Times.Once);

        var json = File.ReadAllText("flyio.generated.json");
        var parsedJson = JsonSerializer.Deserialize<JsonElement>(json);

        parsedJson.GetProperty("app").GetString().Should().Be("somename");
        parsedJson.GetProperty("primary_region").GetString().Should().Be("fra");
        parsedJson.GetProperty("build").GetProperty("image").GetString().Should().Be("registry.fly.io/somename");
    }
}
