using System.Reflection.Metadata;
using Microsoft.AspNetCore.SignalR;

namespace Cli.Auth;

public sealed class CliAuthHub : Hub
{
    public const string AuthSuccessKey = "AuthSuccess";

    private static readonly Dictionary<string, string> _sessions = new();

    public static string? GetConnectionIdFromSessionId(string sessionId)
        => _sessions.TryGetValue(sessionId, out var connectionId) ? connectionId : null;

    public Task InitializeSession(string sessionId)
    {
        _sessions[sessionId] = Context.ConnectionId;

        return Task.CompletedTask;
    }

    public override Task OnDisconnectedAsync(Exception? exception)
    {
        var sessionToRemove = _sessions.FirstOrDefault(x => x.Value == Context.ConnectionId);
        if (!string.IsNullOrEmpty(sessionToRemove.Key))
        {
            _sessions.Remove(sessionToRemove.Key);
        }
        return base.OnDisconnectedAsync(exception);
    }
}