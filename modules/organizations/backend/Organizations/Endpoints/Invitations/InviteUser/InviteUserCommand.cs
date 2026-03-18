using Core.MediatR;
using Organizations.Abstractions;
using Organizations.Models;

namespace Organizations.Endpoints.Invitations.InviteUser;

public sealed record InviteUserCommand(OrganizationId OrganizationId, string Email, OrganizationRole Role) : ICommand<InviteUserResponse>;
public sealed record InviteUserResponse(Guid InvitationId);
