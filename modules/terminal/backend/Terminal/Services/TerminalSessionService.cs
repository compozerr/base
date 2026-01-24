using System.Text;
using Api.Abstractions;
using Api.Data;
using Auth.Abstractions;
using MediatR;
using Terminal.Events;

namespace Terminal.Services;

public sealed class TerminalSessionService : ITerminalSessionService
{
    private readonly ApiDbContext _dbContext;
    private readonly IPublisher _publisher;

    // Buffer for accumulating input to detect commands (newline-terminated)
    private readonly Dictionary<TerminalSessionId, StringBuilder> _inputBuffers = new();
    private readonly Dictionary<TerminalSessionId, (ProjectId ProjectId, UserId UserId)> _sessionContext = new();

    public TerminalSessionService(ApiDbContext dbContext, IPublisher publisher)
    {
        _dbContext = dbContext;
        _publisher = publisher;
    }

    public async Task<TerminalSession> CreateSessionAsync(ProjectId projectId, UserId userId)
    {
        var session = new TerminalSession
        {
            Id = TerminalSessionId.Create(Guid.NewGuid()),
            ProjectId = projectId,
            UserId = userId,
            StartedAtUtc = DateTime.UtcNow
        };

        _dbContext.TerminalSessions.Add(session);
        await _dbContext.SaveChangesAsync();

        // Initialize input buffer for this session
        _inputBuffers[session.Id] = new StringBuilder();
        _sessionContext[session.Id] = (projectId, userId);

        return session;
    }

    public async Task EndSessionAsync(TerminalSessionId sessionId, string? reason = null)
    {
        var session = await _dbContext.TerminalSessions.FindAsync(sessionId);
        if (session != null && session.EndedAtUtc == null)
        {
            session.EndedAtUtc = DateTime.UtcNow;
            session.DisconnectReason = reason;
            await _dbContext.SaveChangesAsync();
        }

        // Clean up buffers
        _inputBuffers.Remove(sessionId);
        _sessionContext.Remove(sessionId);
    }

    public async Task ProcessInputAsync(TerminalSessionId sessionId, string input)
    {
        if (!_inputBuffers.TryGetValue(sessionId, out var buffer) ||
            !_sessionContext.TryGetValue(sessionId, out var context))
        {
            return;
        }

        buffer.Append(input);

        // Check for complete commands (newline-terminated)
        var content = buffer.ToString();
        var lastNewline = content.LastIndexOf('\n');

        if (lastNewline >= 0)
        {
            // Extract complete commands
            var completeInput = content[..(lastNewline + 1)];
            var remaining = content[(lastNewline + 1)..];

            // Update buffer with remaining incomplete input
            buffer.Clear();
            buffer.Append(remaining);

            // Process each line as a potential command
            var lines = completeInput.Split('\n', StringSplitOptions.RemoveEmptyEntries);
            foreach (var line in lines)
            {
                var command = line.Trim('\r', '\n', ' ');
                if (!string.IsNullOrWhiteSpace(command))
                {
                    await _publisher.Publish(new TerminalCommandExecutedEvent(
                        sessionId,
                        context.ProjectId,
                        context.UserId,
                        command,
                        DateTime.UtcNow));
                }
            }
        }
    }
}
