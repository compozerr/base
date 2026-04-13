using Api.EventHandlers.Stripe.PaymentMethodMissingActions.Core;
using Auth.Models;
using Core.Services;
using Mail;
using Mail.Services;
using Organizations.Models;
using Stripe.Events;

namespace Api.EventHandlers.Stripe.PaymentMethodMissingActions;

public sealed class ThirdAndTerminationMail_PaymentMethodMissingAction(
    PaymentMethodMissingEvent @event,
    User user,
    Organization organization,
    IServiceProvider serviceProvider
) : BasePaymentMethodMissingAction(@event, user, organization, serviceProvider)
{
    private readonly Serilog.ILogger _logger = Log.Logger.ForContext<ThirdAndTerminationMail_PaymentMethodMissingAction>();
    public override async Task ExecuteAsync(CancellationToken cancellationToken = default)
    {
        using var scope = ServiceProvider.CreateScope();
        var mailService = scope.ServiceProvider.GetRequiredService<IMailService>();
        var frontendLocation = scope.ServiceProvider.GetRequiredService<IFrontendLocation>();

        try
        {
            var mail = await ReactEmail.CreateAsync(
                new EmailAddress("hosting-noreply@notifications.compozerr.com", "compozerr hosting"),
                [new EmailAddress(User.Email, User.Name)],
                "Important Notice: Your Compozerr Projects Have Been Terminated",
                new Emails.AllProjectsHaveBeenTerminatedTemplate()
                {
                    CompanyName = "compozerr hosting",
                    Reason = $"Due to missing payment method, we have regrettably terminated all projects associated with {GetOrgDisplayLabel()}.",
                    CustomerName = User.Name,
                    DashboardLink = frontendLocation.GetFromPath("/projects").ToString(),
                    ContactLink = frontendLocation.GetFromPath("/contact").ToString(),
                    CompanyAddress = "Vilh. Bergsøes Vej 11, 8210 Aarhus V, Denmark"
                });

            await mailService.SendEmailAsync(mail);

            _logger.Information("Successfully sent all projects termination notification for customer: {CustomerId}", Event.CustomerId);
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Error handling all projects termination for customer: {CustomerId}", Event.CustomerId);
            throw;
        }
    }
}