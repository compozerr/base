namespace Api.Endpoints.Projects.Deployments.Logs.Get;

public sealed record LogEntry(
    DateTime Timestamp,
    LogEntryLevel Level,
    string Message);
