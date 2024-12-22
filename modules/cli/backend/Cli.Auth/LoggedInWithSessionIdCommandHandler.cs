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
        if (!CliAuthHub.HasConnectionWithSessionId(command.SessionId))
        {
            Log.ForContext(nameof(command.SessionId), command.SessionId)
               .Warning("No session with given sessionId was found");

            throw new InvalidOperationException("Session not found");
        }

        await hubContext.Clients.Client(command.SessionId).SendAsync(CliAuthHub.AuthSuccessKey, new
        {
            token = command.Token,
            expiresAtUtc = command.ExpiresAtUtc.ToString("s", CultureInfo.InvariantCulture)
        }, cancellationToken);
    }
}