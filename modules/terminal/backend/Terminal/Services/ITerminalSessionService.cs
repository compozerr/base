using Api.Abstractions;
using Api.Data;
using Auth.Abstractions;

namespace Terminal.Services;

public interface ITerminalSessionService
{
    Task<TerminalSession> CreateSessionAsync(ProjectId projectId, UserId userId);
    Task EndSessionAsync(TerminalSessionId sessionId, string? reason = null);
    Task ProcessInputAsync(TerminalSessionId sessionId, string input);
}
