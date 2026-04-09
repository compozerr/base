using Api.Emails;
using Auth.Repositories;
using Core.Abstractions;
using Core.Services;
using Mail;
using Mail.Services;
using Organizations.Data.Repositories;
using Organizations.Events;
using Serilog;

namespace Api.EventHandlers.Organizations;

public sealed class SendInvitationEmail_InvitationSentEventHandler(
    IOrganizationInvitationRepository invitationRepository,
    IOrganizationRepository organizationRepository,
    IUserRepository userRepository,
    IServiceProvider serviceProvider) : EntityDomainEventHandlerBase<InvitationSentEvent>
{
    private static readonly Serilog.ILogger Logger = Log.ForContext<SendInvitationEmail_InvitationSentEventHandler>();

    protected override async Task HandleAfterSaveAsync(InvitationSentEvent domainEvent, CancellationToken cancellationToken)
    {
        using var scope = serviceProvider.CreateScope();
        var mailService = scope.ServiceProvider.GetRequiredService<IMailService>();
        var frontendLocation = scope.ServiceProvider.GetRequiredService<IFrontendLocation>();

        var invitation = await invitationRepository.GetByIdAsync(domainEvent.InvitationId, cancellationToken);
        if (invitation is null) return;

        // No email to send to — GitHub username-only invite will be auto-accepted on login
        if (string.IsNullOrWhiteSpace(invitation.Email)) return;

        var organization = await organizationRepository.GetByIdAsync(invitation.OrganizationId, cancellationToken);
        if (organization is null) return;

        var inviter = await userRepository.GetByIdAsync(invitation.InvitedByUserId, cancellationToken);

        var acceptLink = frontendLocation.GetFromPath($"invitations/{invitation.Token}").ToString();

        var mail = await ReactEmail.CreateAsync(
            new EmailAddress("hosting-noreply@notifications.compozerr.com", "compozerr hosting"),
            [new EmailAddress(invitation.Email)],
            $"You've been invited to join {organization.Name} on compozerr",
            new OrganizationInvitationTemplate
            {
                CompanyName = "compozerr hosting",
                OrganizationName = organization.Name,
                InviterName = inviter?.Name ?? "A team member",
                Role = invitation.Role.ToString(),
                AcceptLink = acceptLink,
                CompanyAddress = "Vilh. Bergs\u00f8es Vej 11, 8210 Aarhus V, Denmark"
            });

        await mailService.SendEmailAsync(mail);

        Logger.Information("Sent organization invitation email to {Email} for org {OrgId}",
            invitation.Email, invitation.OrganizationId);
    }
}
