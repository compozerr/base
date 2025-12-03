using Api.EventHandlers.Stripe.PaymentFailedActions.Core;
using Auth.Models;
using Core.Services;
using Mail;
using Mail.Services;
using Stripe.Events;

namespace Api.EventHandlers.Stripe.PaymentFailedActions;

public sealed class OnboardingMail_PaymentFailedAction(
    PaymentFailedEvent @event,
    User user,
    IServiceProvider serviceProvider
) : BasePaymentFailedAction(@event, user, serviceProvider)
{
    private readonly Serilog.ILogger _logger = Log.Logger.ForContext<OnboardingMail_PaymentFailedAction>();

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
                "Important: Invoice Payment Failed - Action Required",
                new Emails.FailedInvoiceOnboardingTemplate()
                {
                    CustomerName = User.Name,
                    AmountDue = Event.AmountDue.ToString("F2"),
                    Currency = Event.Currency.ToUpper(),
                    DueDate = Event.DueDate.ToString("MMMM dd, yyyy"),
                    DaysOverdue = Event.DaysOverdue.ToString(),
                    PaymentLink = Event.PaymentLink,
                    DashboardLink = frontendLocation.GetFromPath("/projects").ToString(),
                    ContactLink = frontendLocation.GetFromPath("/contact").ToString(),
                    CompanyName = "compozerr hosting",
                    CompanyAddress = "Vilh. Bergsøes Vej 11, 8210 Aarhus V, Denmark"
                });

            await mailService.SendEmailAsync(mail);

            _logger.Information("Successfully sent failed invoice onboarding email for invoice: {InvoiceId}", Event.InvoiceId);
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Error sending failed invoice onboarding email for invoice: {InvoiceId}", Event.InvoiceId);
            throw;
        }
    }
}
