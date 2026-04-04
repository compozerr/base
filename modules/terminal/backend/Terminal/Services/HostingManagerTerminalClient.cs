using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using Api.Data;
using Api.Hosting.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Serilog;

namespace Terminal.Services;

public sealed class HostingManagerTerminalClient : IHostingManagerTerminalClient, IDisposable
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ICryptoService _cryptoService;
    private readonly IHttpClientFactory _httpClientFactory;

    private ClientWebSocket? _webSocket;
    private CancellationTokenSource? _receiveCts;
    private Task? _receiveTask;

    public HostingManagerTerminalClient(
        IServiceScopeFactory scopeFactory,
        ICryptoService cryptoService,
        IHttpClientFactory httpClientFactory)
    {
        _scopeFactory = scopeFactory;
        _cryptoService = cryptoService;
        _httpClientFactory = httpClientFactory;
    }

    public async Task<bool> IsVmRunningAsync(string apiDomain, string vmIdentifier)
    {
        try
        {
            // Get server for signing
            await using var scope = _scopeFactory.CreateAsyncScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<ApiDbContext>();
            var server = await dbContext.Servers.FirstOrDefaultAsync(s => s.ApiDomain == apiDomain);
            if (server == null) return false;

            var httpClient = _httpClientFactory.CreateClient();
            httpClient.BaseAddress = new Uri(apiDomain);

            var signature = _cryptoService.SignStringWithKey("", server.Id.Value.ToString());

            var request = new HttpRequestMessage(HttpMethod.Get, $"/terminal/status/{vmIdentifier}");
            request.Headers.Add("x-signature", signature);

            var response = await httpClient.SendAsync(request);
            if (!response.IsSuccessStatusCode) return false;

            var content = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<VmStatusResult>(content, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            return result?.Running ?? false;
        }
        catch (Exception ex)
        {
            Log.ForContext("VmIdentifier", vmIdentifier)
               .ForContext("Error", ex.Message)
               .Error("Failed to check VM status");
            return false;
        }
    }

    public async Task ConnectAsync(
        string apiDomain,
        string vmIdentifier,
        Func<string, Task> onData,
        Func<string, Task> onDisconnected,
        Func<string, Task> onError)
    {
        // Get server for signing
        await using var scope = _scopeFactory.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApiDbContext>();
        var server = await dbContext.Servers.FirstOrDefaultAsync(s => s.ApiDomain == apiDomain);
        if (server == null)
        {
            throw new InvalidOperationException($"Server not found for domain: {apiDomain}");
        }

        // Create signed WebSocket connection
        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString();
        var signatureContent = $"{vmIdentifier}:{timestamp}";
        var signature = _cryptoService.SignStringWithKey(signatureContent, server.Id.Value.ToString());

        // Convert https:// URL to wss:// WebSocket URL
        var apiUri = new Uri(apiDomain);
        var wsUri = new Uri($"wss://{apiUri.Host}/terminal/{vmIdentifier}?timestamp={timestamp}&signature={Uri.EscapeDataString(signature)}");

        _webSocket = new ClientWebSocket();
        _receiveCts = new CancellationTokenSource();

        await _webSocket.ConnectAsync(wsUri, CancellationToken.None);

        // Start receiving messages
        _receiveTask = ReceiveLoopAsync(onData, onDisconnected, onError, _receiveCts.Token);
    }

    private async Task ReceiveLoopAsync(
        Func<string, Task> onData,
        Func<string, Task> onDisconnected,
        Func<string, Task> onError,
        CancellationToken cancellationToken)
    {
        var buffer = new byte[8192];

        try
        {
            while (_webSocket?.State == WebSocketState.Open && !cancellationToken.IsCancellationRequested)
            {
                var result = await _webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), cancellationToken);

                if (result.MessageType == WebSocketMessageType.Close)
                {
                    await onDisconnected(result.CloseStatusDescription ?? "Connection closed");
                    break;
                }

                if (result.MessageType == WebSocketMessageType.Text)
                {
                    var message = Encoding.UTF8.GetString(buffer, 0, result.Count);
                    Log.ForContext("MessageLength", message.Length)
                        .Information("Received message from hosting");

                    var parsed = JsonSerializer.Deserialize<TerminalMessage>(message, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });

                    if (parsed != null)
                    {
                        Log.ForContext("MessageType", parsed.Type)
                            .ForContext("PayloadLength", parsed.Payload?.Length ?? 0)
                            .Information("Parsed terminal message");

                        switch (parsed.Type)
                        {
                            case "data":
                                if (parsed.Payload != null)
                                {
                                    Log.ForContext("DataLength", parsed.Payload.Length)
                                        .Information("Forwarding terminal data to client");
                                    await onData(parsed.Payload);
                                }
                                break;
                            case "disconnected":
                                await onDisconnected(parsed.Reason ?? "Disconnected");
                                return;
                            case "error":
                                await onError(parsed.Message ?? "Unknown error");
                                break;
                        }
                    }
                }
            }
        }
        catch (OperationCanceledException)
        {
            // Normal cancellation
        }
        catch (WebSocketException ex)
        {
            await onError($"WebSocket error: {ex.Message}");
            await onDisconnected("WebSocket error");
        }
        catch (Exception ex)
        {
            Log.ForContext("Error", ex.Message).Error("Error in terminal receive loop");
            await onError($"Error: {ex.Message}");
            await onDisconnected("Error");
        }
    }

    public async Task SendInputAsync(string data)
    {
        if (_webSocket?.State != WebSocketState.Open) return;

        var message = JsonSerializer.Serialize(new { type = "data", payload = data });
        var bytes = Encoding.UTF8.GetBytes(message);

        await _webSocket.SendAsync(new ArraySegment<byte>(bytes), WebSocketMessageType.Text, true, CancellationToken.None);
    }

    public async Task ResizeTerminalAsync(int cols, int rows)
    {
        if (_webSocket?.State != WebSocketState.Open) return;

        var message = JsonSerializer.Serialize(new { type = "resize", cols, rows });
        var bytes = Encoding.UTF8.GetBytes(message);

        await _webSocket.SendAsync(new ArraySegment<byte>(bytes), WebSocketMessageType.Text, true, CancellationToken.None);
    }

    public async Task DisconnectAsync()
    {
        _receiveCts?.Cancel();

        if (_webSocket?.State == WebSocketState.Open)
        {
            try
            {
                await _webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Client disconnected", CancellationToken.None);
            }
            catch
            {
                // Ignore errors during close
            }
        }

        _webSocket?.Dispose();
        _webSocket = null;
    }

    public void Dispose()
    {
        // Intentionally empty: this client outlives the DI scope that created it.
        // It is stored in the singleton TerminalConnectionManager and cleaned up
        // via DisconnectAsync() when the SignalR connection closes.
    }

    private record VmStatusResult(bool Running);

    private record TerminalMessage(
        string Type,
        string? Payload = null,
        string? Reason = null,
        string? Message = null);
}
