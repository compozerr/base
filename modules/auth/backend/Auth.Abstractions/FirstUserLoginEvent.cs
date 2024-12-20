namespace Auth.Abstractions;

public record FirstUserLoginEvent(UserId UserId) : IDomainEvent;
