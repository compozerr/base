namespace Terminal.Services;

public interface IHostingManagerTerminalClient
{
    Task<bool> IsVmRunningAsync(string apiDomain, string vmIdentifier);

    Task ConnectAsync(
        string apiDomain,
        string vmIdentifier,
        Func<string, Task> onData,
        Func<string, Task> onDisconnected,
        Func<string, Task> onError);

    Task SendInputAsync(string data);
    Task ResizeTerminalAsync(int cols, int rows);
    Task DisconnectAsync();
}
