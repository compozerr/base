using Api.Data;
using Api.Data.Events;
using Api.Data.Repositories;
using Api.Emails;
using Auth.Repositories;
using Core.Abstractions;
using Core.Services;
using Mail;
using Mail.Services;
using Microsoft.EntityFrameworkCore;

namespace Api.EventHandlers.Domains;

public class SendDomainVerifiedEmail_DomainVerifiedEventHandler(
    IMailService mailService,
    IUserRepository userRepository,
    IDomainRepository domainRepository,
    IFrontendLocation frontendLocation) : EntityDomainEventHandlerBase<DomainVerifiedEvent>
{
    private readonly Serilog.ILogger _logger = Log.Logger.ForContext<SendDomainVerifiedEmail_DomainVerifiedEventHandler>();

    protected override async Task HandleAfterSaveAsync(DomainVerifiedEvent domainEvent, CancellationToken cancellationToken)
    {
        try
        {
            // Get domain with project info
            var domain = await domainRepository.Query()
                .Include(d => d.Project)
                .FirstOrDefaultAsync(d => d.Id == domainEvent.Entity.Id, cancellationToken);

            if (domain?.Project == null)
            {
                _logger.Warning("Domain {DomainId} or its project not found. Cannot send verification email.",
                    domainEvent.Entity.Id);
                return;
            }

            var user = await userRepository.GetByIdAsync(domain.Project.UserId, cancellationToken);

            if (user == null)
            {
                _logger.Warning("User {UserId} not found for domain {DomainId}. Cannot send verification email.",
                    domain.Project.UserId, domain.Id);
                return;
            }

            var domainValue = domain.GetValue;
            var projectSettingsLink = frontendLocation.GetFromPath($"/projects/{domain.ProjectId}/settings").ToString();

            var mail = await ReactEmail.CreateAsync(
                new EmailAddress("hosting-noreply@notifications.compozerr.com", "Compozerr Hosting"),
                [new EmailAddress(user.Email, user.Name)],
                $"Your domain {domainValue} has been verified",
                new DomainVerifiedTemplate
                {
                    CompanyName = "compozerr hosting",
                    CustomerName = user.Name,
                    DomainName = domainValue,
                    ProjectName = domain.Project.Name,
                    DashboardLink = projectSettingsLink,
                    DomainLink = $"https://{domainValue}",
                    CompanyAddress = "Vilh. Bergsoes Vej 11, 8210 Aarhus V, Denmark",
                });

            await mailService.SendEmailAsync(mail);

            _logger.Information("Successfully sent domain verified notification for domain: {DomainId} ({DomainValue})",
                domain.Id, domainValue);
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Error sending domain verified email for domain: {DomainId}", domainEvent.Entity.Id);
            throw;
        }
    }
}
