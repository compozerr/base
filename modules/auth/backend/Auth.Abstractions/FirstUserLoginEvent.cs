namespace Auth.Abstractions;

public record FirstUserLoginEvent(UserId UserId, string AccessToken) : IDomainEvent;
