namespace Cli.Services.TemplateDetection;

public sealed class TemplateDetectorService : ITemplateDetectorService
{
    private static readonly IReadOnlyList<TemplatePattern> Patterns =
    [
        new TemplatePattern(
            TemplateType.DockerCompose,
            "docker-compose.yml",
            "docker compose up -d"),
        new TemplatePattern(
            TemplateType.DockerCompose,
            "docker-compose.yaml",
            "docker compose up -d"),
        new TemplatePattern(
            TemplateType.DockerCompose,
            "compose.yml",
            "docker compose up -d"),
        new TemplatePattern(
            TemplateType.DockerCompose,
            "compose.yaml",
            "docker compose up -d")
    ];

    public IReadOnlyList<DetectedTemplate> DetectTemplates(string rootPath)
    {
        var templates = new List<DetectedTemplate>();

        if (!Directory.Exists(rootPath))
        {
            return templates;
        }

        foreach (var pattern in Patterns)
        {
            var filePath = Path.Combine(rootPath, pattern.FileName);

            if (File.Exists(filePath))
            {
                templates.Add(new DetectedTemplate(
                    pattern.Type,
                    pattern.FileName,
                    pattern.StartCommand));
            }
        }

        return templates;
    }

    private sealed record TemplatePattern(
        TemplateType Type,
        string FileName,
        string StartCommand);
}
