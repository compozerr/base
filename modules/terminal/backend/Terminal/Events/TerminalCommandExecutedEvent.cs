using Api.Abstractions;
using Auth.Abstractions;
using MediatR;

namespace Terminal.Events;

public sealed record TerminalCommandExecutedEvent(
    TerminalSessionId SessionId,
    ProjectId ProjectId,
    UserId UserId,
    string Command,
    DateTime ExecutedAtUtc) : INotification;
