using Core.Abstractions;

namespace Mail.Events;

public sealed record SendEmailEvent(
    Email Email) : IEvent;