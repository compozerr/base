using Auth.Abstractions;
using Core.Abstractions;
using Organizations.Abstractions;

namespace Organizations.Events;

public record InvitationAcceptedEvent(
    OrganizationId OrganizationId,
    UserId UserId,
    OrganizationInvitationId InvitationId) : IDomainEvent;
