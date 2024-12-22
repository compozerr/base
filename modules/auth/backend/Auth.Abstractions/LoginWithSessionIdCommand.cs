using Core.MediatR;

namespace Auth.Abstractions;

public sealed record LoginWithSessionIdCommand(string SessionId) : ICommand;