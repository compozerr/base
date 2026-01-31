using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using Api.Abstractions;
using Api.Data;
using Api.Hosting.Services;
using Microsoft.EntityFrameworkCore;
using Serilog;

namespace Terminal.Services;

public sealed class HostingManagerTerminalClient : IHostingManagerTerminalClient, IDisposable
{
    private readonly ApiDbContext _dbContext;
    private readonly ICryptoService _cryptoService;
    private readonly IHttpClientFactory _httpClientFactory;

    private ClientWebSocket? _webSocket;
    private CancellationTokenSource? _receiveCts;
    private Task? _receiveTask;

    public HostingManagerTerminalClient(
        ApiDbContext dbContext,
        ICryptoService cryptoService,
        IHttpClientFactory httpClientFactory)
    {
        _dbContext = dbContext;
        _cryptoService = cryptoService;
        _httpClientFactory = httpClientFactory;
    }

    public async Task<bool> IsVmRunningAsync(string apiDomain, ProjectId projectId)
    {
        try
        {
            // Get server for signing
            var server = await _dbContext.Servers.FirstOrDefaultAsync(s => s.ApiDomain == apiDomain);
            if (server == null) return false;

            var httpClient = _httpClientFactory.CreateClient();
            httpClient.BaseAddress = new Uri(apiDomain);

            var signature = _cryptoService.SignStringWithKey("", server.Id.Value.ToString());

            var request = new HttpRequestMessage(HttpMethod.Get, $"/terminal/status/{projectId.Value}");
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
            Log.ForContext("ProjectId", projectId)
               .ForContext("Error", ex.Message)
               .Error("Failed to check VM status");
            return false;
        }
    }

    public async Task ConnectAsync(
        string apiDomain,
        ProjectId projectId,
        Func<string, Task> onData,
        Func<string, Task> onDisconnected,
        Func<string, Task> onError)
    {
        // Get server for signing
        var server = await _dbContext.Servers.FirstOrDefaultAsync(s => s.ApiDomain == apiDomain);
        if (server == null)
        {
            throw new InvalidOperationException($"Server not found for domain: {apiDomain}");
        }

        // Create signed WebSocket connection
        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString();
        var signatureContent = $"{projectId.Value}:{timestamp}";
        var signature = _cryptoService.SignStringWithKey(signatureContent, server.Id.Value.ToString());

        // Convert https:// URL to wss:// WebSocket URL
        var apiUri = new Uri(apiDomain);
        var wsUri = new Uri($"wss://{apiUri.Host}/terminal/{projectId.Value}?timestamp={timestamp}&signature={Uri.EscapeDataString(signature)}");

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
                    var parsed = JsonSerializer.Deserialize<TerminalMessage>(message, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });

                    if (parsed != null)
                    {
                        switch (parsed.Type)
                        {
                            case "data":
                                if (parsed.Payload != null)
                                {
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
        _receiveCts?.Cancel();
        _receiveCts?.Dispose();
        _webSocket?.Dispose();
    }

    private record VmStatusResult(bool Running);

    private record TerminalMessage(
        string Type,
        string? Payload = null,
        string? Reason = null,
        string? Message = null);
}
