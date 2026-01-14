using MediatR;
using Serilog;

namespace Terminal.Events;

public sealed class TerminalCommandExecutedEventHandler : INotificationHandler<TerminalCommandExecutedEvent>
{
    public Task Handle(TerminalCommandExecutedEvent notification, CancellationToken cancellationToken)
    {
        Log.ForContext("SessionId", notification.SessionId.Value)
           .ForContext("ProjectId", notification.ProjectId.Value)
           .ForContext("UserId", notification.UserId.Value)
           .Information("Terminal command executed: {Command}", notification.Command);

        return Task.CompletedTask;
    }
}
