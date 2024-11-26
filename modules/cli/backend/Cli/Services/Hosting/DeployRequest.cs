
namespace Cli.Services.Hosting;

public record DeployRequest(string AppName, string RegistryPath, Platform Platform);
