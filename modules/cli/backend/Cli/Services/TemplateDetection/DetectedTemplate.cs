namespace Cli.Services.TemplateDetection;

public sealed record DetectedTemplate(
    TemplateType Type,
    string FilePath,
    string StartCommand);

public enum TemplateType
{
    DockerCompose
}
