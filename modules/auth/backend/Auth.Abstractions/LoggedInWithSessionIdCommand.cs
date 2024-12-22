using Core.MediatR;

namespace Auth.Abstractions;

public sealed record LoggedInWithSessionIdCommand(string SessionId, string Token, DateTime ExpiresAtUtc) : ICommand;