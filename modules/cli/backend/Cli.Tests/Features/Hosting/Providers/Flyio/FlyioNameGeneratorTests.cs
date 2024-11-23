using Cli.Features.Hosting.Providers.Flyio;

namespace Cli.Tests.Features.Hosting.Providers.Flyio;

public class FlyioNameGeneratorTests
{
    [Fact]
    public void GenerateName_should_generate_correct_name()
    {
        // Arrange
        var generator = new FlyioNameGenerator();
        var appName = "my-app";
        var platform = Platform.Backend;

        // Act
        var name = generator.GenerateName(appName, platform);

        // Assert
        Assert.NotNull(name);
        Assert.True(name.Length > 0);

        Console.WriteLine(name);

        var parts = name.Split('-');

        Assert.Equal(3, parts.Length);

        Assert.Equal("backend", parts[0]);
        Assert.Equal("myapp", parts[1]);
        Assert.True(parts[2].Length == 6);
    }
}
