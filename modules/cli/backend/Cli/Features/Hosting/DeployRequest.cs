
namespace Cli.Features.Hosting;

public record DeployRequest(string AppName, string RegistryPath, Platform Platform);
