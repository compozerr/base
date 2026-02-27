using System.Collections.Concurrent;
using Api.Abstractions;
using Api.Data;
using Auth.Abstractions;
using Auth.Services;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using Terminal.Events;
using Terminal.Services;

namespace Terminal;

public sealed record StartSessionResult(
    bool Success,
    string? SessionId = null,
    string? Error = null,
    bool VmRunning = true);

[Authorize]
public sealed class TerminalHub : Hub
{
    private readonly ApiDbContext _dbContext;
    private readonly ICurrentUserAccessor _currentUserAccessor;
    private readonly ITerminalSessionService _sessionService;
    private readonly IHostingManagerTerminalClient _hostingClient;
    private readonly ITerminalConnectionManager _connectionManager;
    private readonly IPublisher _publisher;
    private readonly IHubContext<TerminalHub> _hubContext;
    private readonly IServiceScopeFactory _scopeFactory;

    // Track active sessions by connection ID
    private static readonly ConcurrentDictionary<string, TerminalSessionId> _activeSessions = new();

    public TerminalHub(
        ApiDbContext dbContext,
        ICurrentUserAccessor currentUserAccessor,
        ITerminalSessionService sessionService,
        IHostingManagerTerminalClient hostingClient,
        ITerminalConnectionManager connectionManager,
        IPublisher publisher,
        IHubContext<TerminalHub> hubContext,
        IServiceScopeFactory scopeFactory)
    {
        _dbContext = dbContext;
        _currentUserAccessor = currentUserAccessor;
        _sessionService = sessionService;
        _hostingClient = hostingClient;
        _connectionManager = connectionManager;
        _publisher = publisher;
        _hubContext = hubContext;
        _scopeFactory = scopeFactory;
    }

    public async Task<StartSessionResult> StartSession(string projectIdString)
    {
        var userId = _currentUserAccessor.CurrentUserId;
        if (userId == null)
        {
            return new StartSessionResult(false, Error: "Not authenticated");
        }

        if (!Guid.TryParse(projectIdString, out var projectGuid))
        {
            return new StartSessionResult(false, Error: "Invalid project ID format");
        }

        var projectId = ProjectId.Create(projectGuid);

        // Get project and verify ownership
        var project = await _dbContext.Projects
            .Include(p => p.Server)
            .FirstOrDefaultAsync(p => p.Id == projectId && p.UserId == userId);

        if (project == null)
        {
            return new StartSessionResult(false, Error: "Project not found or access denied");
        }

        if (project.Server == null)
        {
            return new StartSessionResult(false, Error: "Project has no assigned server");
        }

        // Check VM status via hosting manager
        var vmRunning = await _hostingClient.IsVmRunningAsync(project.Server.ApiDomain, projectId);
        if (!vmRunning)
        {
            return new StartSessionResult(false, Error: "VM is not running", VmRunning: false);
        }

        // Create session record
        var session = await _sessionService.CreateSessionAsync(projectId, userId);

        // Store session mapping
        _activeSessions[Context.ConnectionId] = session.Id;

        // Connect to hosting manager terminal WebSocket
        try
        {
            var connectionId = Context.ConnectionId;
            var scopeFactory = _scopeFactory;
            await _hostingClient.ConnectAsync(
                project.Server.ApiDomain,
                projectId,
                onData: async (data) => await _hubContext.Clients.Client(connectionId).SendAsync("OnOutput", data),
                onDisconnected: async (reason) =>
                {
                    // Create a fresh scope — the original hub scope is long disposed
                    await using var scope = scopeFactory.CreateAsyncScope();
                    var sessionService = scope.ServiceProvider.GetRequiredService<ITerminalSessionService>();
                    await sessionService.EndSessionAsync(session.Id, reason);
                    _activeSessions.TryRemove(connectionId, out _);
                    await _connectionManager.RemoveClientAsync(connectionId);
                    await _hubContext.Clients.Client(connectionId).SendAsync("OnDisconnected", reason);
                },
                onError: async (error) => await _hubContext.Clients.Client(connectionId).SendAsync("OnError", error)
            );

            // Store the connected client so subsequent calls can use it
            _connectionManager.StoreClient(connectionId, _hostingClient);

            return new StartSessionResult(true, SessionId: session.Id.Value.ToString());
        }
        catch (Exception ex)
        {
            Log.ForContext("ProjectId", projectId)
                .ForContext("Error", ex.Message)
                .Error("Failed to connect to terminal");

            await _sessionService.EndSessionAsync(session.Id, $"Connection failed: {ex.Message}");
            _activeSessions.TryRemove(Context.ConnectionId, out _);

            return new StartSessionResult(false, Error: "Failed to connect to terminal");
        }
    }

    public async Task SendInput(string data)
    {
        if (!_activeSessions.TryGetValue(Context.ConnectionId, out var sessionId))
        {
            return;
        }

        // Get the client for this connection
        var client = _connectionManager.GetClient(Context.ConnectionId);
        if (client == null)
        {
            return;
        }

        // Forward input to hosting manager
        await client.SendInputAsync(data);

        // Parse for commands (look for newlines)
        await _sessionService.ProcessInputAsync(sessionId, data);
    }

    public async Task ResizeTerminal(int cols, int rows)
    {
        if (!_activeSessions.ContainsKey(Context.ConnectionId))
        {
            return;
        }

        // Get the client for this connection
        var client = _connectionManager.GetClient(Context.ConnectionId);
        if (client == null)
        {
            return;
        }

        await client.ResizeTerminalAsync(cols, rows);
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        if (_activeSessions.TryRemove(Context.ConnectionId, out var sessionId))
        {
            await _sessionService.EndSessionAsync(sessionId, "Client disconnected");
            await _connectionManager.RemoveClientAsync(Context.ConnectionId);
        }

        await base.OnDisconnectedAsync(exception);
    }
}
