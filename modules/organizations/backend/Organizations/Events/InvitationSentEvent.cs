using Core.Abstractions;
using Organizations.Abstractions;

namespace Organizations.Events;

public record InvitationSentEvent(OrganizationInvitationId InvitationId) : IDomainEvent;
