using Api.Abstractions;

namespace Api.Abstractions.Helpers;

public static class LogHelpers
{
    public static string GetLogEntry(string log, LogLevel level)
           => $"[{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}] [{level.ToString().ToUpper()}] {log}";

    public static string GetLogFileName(DeploymentId deploymentId)
        => $"deployment-logs/{deploymentId}.log";
}