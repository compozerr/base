using System.Globalization;
using Auth.Abstractions;
using Core.MediatR;
using Microsoft.AspNetCore.SignalR;
using Serilog;

namespace Cli.Auth;

public class LoggedInWithSessionIdCommandHandler(
    IHubContext<CliAuthHub> hubContext) : ICommandHandler<LoggedInWithSessionIdCommand>
{
    public async Task Handle(LoggedInWithSessionIdCommand command, CancellationToken cancellationToken = default)
    {
        var connectionId = CliAuthHub.GetConnectionIdFromSessionId(command.SessionId);

        if (string.IsNullOrEmpty(connectionId))
        {
            Log.ForContext(nameof(command.SessionId), command.SessionId)
               .Warning("No connection with given sessionId was found");

            throw new InvalidOperationException("Session not found");
        }

        await hubContext.Clients.Client(connectionId).SendAsync(CliAuthHub.AuthSuccessKey, new
        {
            token = command.Token,
            expiresAtUtc = command.ExpiresAtUtc.ToString("o", CultureInfo.InvariantCulture)
        }, cancellationToken);
    }
}