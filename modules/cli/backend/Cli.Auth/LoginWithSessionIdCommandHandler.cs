using Auth.Abstractions;
using Core.MediatR;
using Serilog;

namespace Cli.Auth;

public sealed class LoginWithSessionIdCommandHandler : ICommandHandler<LoginWithSessionIdCommand>
{
    public Task Handle(LoginWithSessionIdCommand command, CancellationToken cancellationToken = default)
    {
        Log.ForContext(nameof(command), command, true)
           .Information("User is logging in with session id meaning it is comming from the cli");

        return Task.CompletedTask;
    }
}
