namespace Cli.Endpoints.Modules.Release;

public sealed record ReleaseModuleResponse(
    bool Success,
    string? TagName,
    string? ErrorMessage);
