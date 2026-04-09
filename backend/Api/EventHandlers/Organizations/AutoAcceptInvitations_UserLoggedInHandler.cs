using Auth.Abstractions;
using MediatR;
using Organizations.Data.Repositories;
using Organizations.Events;
using Organizations.Models;
using Serilog;

namespace Api.EventHandlers.Organizations;

public sealed class AutoAcceptInvitations_UserLoggedInHandler(
    IOrganizationInvitationRepository invitationRepository,
    IOrganizationMemberRepository memberRepository) : INotificationHandler<UserLoggedInNotification>
{
    private static readonly Serilog.ILogger Logger = Log.ForContext<AutoAcceptInvitations_UserLoggedInHandler>();

    public async Task Handle(UserLoggedInNotification notification, CancellationToken cancellationToken)
    {
        var pendingInvitations = await invitationRepository.GetPendingByGithubUsernameAsync(
            notification.GithubUsername, cancellationToken);

        foreach (var invitation in pendingInvitations)
        {
            try
            {
                if (await memberRepository.IsMemberAsync(invitation.OrganizationId, notification.UserId, cancellationToken))
                {
                    invitation.Status = InvitationStatus.Accepted;
                    await invitationRepository.UpdateAsync(invitation, cancellationToken);
                    continue;
                }

                var member = new OrganizationMember
                {
                    OrganizationId = invitation.OrganizationId,
                    UserId = notification.UserId,
                    Role = invitation.Role
                };

                member.QueueDomainEvent(new InvitationAcceptedEvent(
                    invitation.OrganizationId, notification.UserId, invitation.Id));

                await memberRepository.AddAsync(member, cancellationToken);

                invitation.Status = InvitationStatus.Accepted;
                await invitationRepository.UpdateAsync(invitation, cancellationToken);

                Logger.Information(
                    "Auto-accepted invitation for GitHub user {GithubUsername} to org {OrgId}",
                    notification.GithubUsername, invitation.OrganizationId);
            }
            catch (Exception ex)
            {
                Logger.Warning(ex,
                    "Failed to auto-accept invitation {InvitationId} for GitHub user {GithubUsername}",
                    invitation.Id, notification.GithubUsername);
            }
        }
    }
}
