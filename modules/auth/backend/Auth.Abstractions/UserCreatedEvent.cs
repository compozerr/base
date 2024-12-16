namespace Auth.Abstractions;

public record UserCreatedEvent(UserId UserId) : IDomainEvent;