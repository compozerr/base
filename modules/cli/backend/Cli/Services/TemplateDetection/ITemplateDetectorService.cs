namespace Cli.Services.TemplateDetection;

public interface ITemplateDetectorService
{
    IReadOnlyList<DetectedTemplate> DetectTemplates(string rootPath);
}
