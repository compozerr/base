using Api.Abstractions.Helpers;
using Core.MediatR;
using Storage;

namespace Api.Endpoints.Projects.Deployments.Logs.Get;

public sealed partial class GetLogCommandHandler(
    IStorageService storageService) : ICommandHandler<GetLogCommand, List<LogEntry>>
{
    public async Task<List<LogEntry>> Handle(GetLogCommand command, CancellationToken cancellationToken = default)
    {
        var logStream = await storageService.DownloadAsync(LogHelpers.GetLogFileName(command.DeploymentId), cancellationToken);
        if (logStream is null) return [];

        logStream.Position = 0; // Reset stream position to ensure we read from the beginning
        using var reader = new StreamReader(logStream);
        var logContent = await reader.ReadToEndAsync(cancellationToken);
        var logEntries = ParseLogEntries(logContent);

        return logEntries;
    }

    private static List<LogEntry> ParseLogEntries(string logContent)
    {
        var entries = new List<LogEntry>();
        var lines = logContent.Split('\n', StringSplitOptions.None);

        for (int i = 0; i < lines.Length; i++)
        {
            var line = lines[i].Trim();
            if (string.IsNullOrWhiteSpace(line)) continue;

            // Check for the standard log format [timestamp] [level] message
            var match = LogEntryRegex().Match(line);
            if (match.Success)
            {
                var timestamp = DateTime.Parse(match.Groups[1].Value);
                var levelString = match.Groups[2].Value;
                var message = match.Groups[3].Value;

                // Parse the log level
                if (Enum.TryParse<LogEntryLevel>(levelString, true, out var level))
                {
                    // For error entries, collect all subsequent lines until we find another timestamp entry
                    // For all log entries, collect all subsequent lines until we find another timestamp entry
                    var messageBuilder = new System.Text.StringBuilder(message);
                    int j = i + 1;
                    while (j < lines.Length && !LogEntryRegex().Match(lines[j]).Success)
                    {
                        if (!string.IsNullOrWhiteSpace(lines[j]))
                        {
                            messageBuilder.AppendLine().Append(lines[j]);
                        }
                        j++;
                    }
                    i = j - 1; // Skip the lines we've consumed
                    message = messageBuilder.ToString();

                    entries.Add(new LogEntry(timestamp, level, message));
                }
            }
        }

        return entries;
    }

    [System.Text.RegularExpressions.GeneratedRegex(@"^\[(\d{4}-\d{2}-\d{2} \d{2}:\d{2}:\d{2})\] \[(INFO|ERROR|SUCCESS|WARNING)\] (.+)$")]
    private static partial System.Text.RegularExpressions.Regex LogEntryRegex();
}
