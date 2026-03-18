using Auth.Abstractions;
using Core.Abstractions;
using Organizations.Abstractions;

namespace Organizations.Events;

public record MemberAddedEvent(OrganizationId OrganizationId, UserId UserId) : IDomainEvent;
