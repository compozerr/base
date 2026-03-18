using Core.Abstractions;
using Organizations.Abstractions;

namespace Organizations.Events;

public record OrganizationCreatedEvent(OrganizationId OrganizationId) : IDomainEvent;
