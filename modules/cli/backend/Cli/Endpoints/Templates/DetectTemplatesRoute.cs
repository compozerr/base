using Cli.Services.TemplateDetection;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;

namespace Cli.Endpoints.Templates;

public sealed record DetectTemplatesRequest(string RootPath);

public sealed record DetectTemplatesResponse(IReadOnlyList<DetectedTemplateDto> Templates);

public sealed record DetectedTemplateDto(
    string Type,
    string FilePath,
    string StartCommand);

public static class DetectTemplatesRoute
{
    public const string Route = "detect";

    public static RouteHandlerBuilder AddDetectTemplatesRoute(this IEndpointRouteBuilder app)
    {
        return app.MapPost(Route, ExecuteAsync);
    }

    public static DetectTemplatesResponse ExecuteAsync(
        DetectTemplatesRequest request,
        ITemplateDetectorService templateDetectorService)
    {
        var templates = templateDetectorService.DetectTemplates(request.RootPath);

        var dtos = templates.Select(t => new DetectedTemplateDto(
            t.Type.ToString(),
            t.FilePath,
            t.StartCommand)).ToList();

        return new DetectTemplatesResponse(dtos);
    }
}
