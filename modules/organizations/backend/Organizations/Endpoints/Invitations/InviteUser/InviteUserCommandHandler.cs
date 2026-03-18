using System.Security.Cryptography;
using Auth.Services;
using Core.MediatR;
using Organizations.Data.Repositories;
using Organizations.Events;
using Organizations.Models;
using Organizations.Services;

namespace Organizations.Endpoints.Invitations.InviteUser;

public sealed class InviteUserCommandHandler(
    IOrganizationInvitationRepository invitationRepository,
    IOrganizationAuthorizationService authService,
    ICurrentUserAccessor currentUserAccessor) : ICommandHandler<InviteUserCommand, InviteUserResponse>
{
    public async Task<InviteUserResponse> Handle(InviteUserCommand command, CancellationToken cancellationToken)
    {
        var userId = currentUserAccessor.CurrentUserId!;
        if (!await authService.IsAdminAsync(command.OrganizationId, userId, cancellationToken))
            throw new UnauthorizedAccessException("Only admins can invite members.");

        // Check for existing pending invitation
        if (await invitationRepository.HasPendingInvitationAsync(command.OrganizationId, command.Email, cancellationToken))
            throw new InvalidOperationException("A pending invitation already exists for this email.");

        var token = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32))
            .Replace("+", "-").Replace("/", "_").TrimEnd('=');

        var invitation = new OrganizationInvitation
        {
            OrganizationId = command.OrganizationId,
            Email = command.Email,
            Role = command.Role,
            InvitedByUserId = userId,
            Status = InvitationStatus.Pending,
            Token = token,
            ExpiresAtUtc = DateTime.UtcNow.AddDays(7)
        };

        invitation.QueueDomainEvent(new InvitationSentEvent(invitation.Id));

        await invitationRepository.AddAsync(invitation, cancellationToken);

        return new InviteUserResponse(invitation.Id.Value);
    }
}
