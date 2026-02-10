using System.Collections.Concurrent;

namespace Terminal.Services;

/// <summary>
/// Manages terminal client connections per SignalR connection ID.
/// This is necessary because SignalR hub methods run in separate DI scopes,
/// so we need a singleton to maintain WebSocket connections across calls.
/// </summary>
public interface ITerminalConnectionManager
{
    void StoreClient(string connectionId, IHostingManagerTerminalClient client);
    IHostingManagerTerminalClient? GetClient(string connectionId);
    Task RemoveClientAsync(string connectionId);
}

public sealed class TerminalConnectionManager : ITerminalConnectionManager
{
    private readonly ConcurrentDictionary<string, IHostingManagerTerminalClient> _clients = new();

    public void StoreClient(string connectionId, IHostingManagerTerminalClient client)
    {
        _clients[connectionId] = client;
    }

    public IHostingManagerTerminalClient? GetClient(string connectionId)
    {
        _clients.TryGetValue(connectionId, out var client);
        return client;
    }

    public async Task RemoveClientAsync(string connectionId)
    {
        if (_clients.TryRemove(connectionId, out var client))
        {
            await client.DisconnectAsync();
        }
    }
}
