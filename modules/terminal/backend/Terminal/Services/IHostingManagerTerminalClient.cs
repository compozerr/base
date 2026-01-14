using Api.Abstractions;

namespace Terminal.Services;

public interface IHostingManagerTerminalClient
{
    Task<bool> IsVmRunningAsync(string apiDomain, ProjectId projectId);

    Task ConnectAsync(
        string apiDomain,
        ProjectId projectId,
        Func<string, Task> onData,
        Func<string, Task> onDisconnected,
        Func<string, Task> onError);

    Task SendInputAsync(string data);
    Task ResizeTerminalAsync(int cols, int rows);
    Task DisconnectAsync();
}
