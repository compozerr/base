using Organizations.Models;

namespace Organizations.Endpoints.Invitations.GetInvitationByToken;

public sealed record GetInvitationByTokenResponse(
    Guid Id,
    Guid OrganizationId,
    string OrganizationName,
    string? InviterName,
    OrganizationRole Role,
    InvitationStatus Status,
    DateTime ExpiresAtUtc,
    bool IsExpired);
