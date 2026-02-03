namespace Cli.Endpoints.Projects.UpdateTemplate;

public sealed record UpdateTemplateResponse(
    bool Success = true,
    string? Message = null);
