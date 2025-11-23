using Api.EventHandlers.Stripe.PaymentFailedActions.Core;
using Auth.Models;
using Core.Services;
using Mail;
using Mail.Services;
using Stripe.Events;
using Stripe.Helpers;

namespace Api.EventHandlers.Stripe.PaymentFailedActions;

public sealed class SecondWarningMail_PaymentFailedAction(
    StripeInvoicePaymentFailedEvent @event,
    User user,
    IServiceProvider serviceProvider
) : BasePaymentFailedAction(@event, user, serviceProvider)
{
    private readonly Serilog.ILogger _logger = Log.Logger.ForContext<SecondWarningMail_PaymentFailedAction>();
    public override async Task ExecuteAsync(CancellationToken cancellationToken = default)
    {
        using var scope = ServiceProvider.CreateScope();
        var mailService = scope.ServiceProvider.GetRequiredService<IMailService>();
        var frontendLocation = scope.ServiceProvider.GetRequiredService<IFrontendLocation>();

        var currencySymbol = CurrencyHelper.GetSymbol(Event.Currency);

        try
        {
            var mail = await ReactEmail.CreateAsync(
                new EmailAddress("no-reply@notifications.compozerr.com", "compozerr hosting"),
                [new EmailAddress(User.Email, User.Name)],
                "Action Required: Payment Failed for Your Compozerr Invoice",
                new Emails.MissingInvoicePaymentTemplate()
                {
                    DueDate = Event.DueDate.ToString("MMMM dd, yyyy"),
                    CompanyName = "compozerr hosting",
                    Description = "This is a second reminder that we have not received payment for the following invoice. Please submit payment to avoid any service interruptions.",
                    CustomerName = User.Name,
                    Currency = currencySymbol,
                    AmountDue = Event.AmountDue.ToString("F2"),
                    DaysOverdue = Event.DaysOverdue.ToString(),
                    PaymentLink = Event.PaymentLink,
                    DashboardLink = frontendLocation.GetFromPath("/projects").ToString(),
                    ContactLink = frontendLocation.GetFromPath("/contact").ToString(),
                    CompanyAddress = "Vilh. Bergsøes Vej 11, 8210 Aarhus V, Denmark"
                });

            await mailService.SendEmailAsync(mail);

            _logger.Information("Successfully sent payment failed notification for invoice: {InvoiceId}", Event.InvoiceId);
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Error handling failed payment for invoice: {InvoiceId}", Event.InvoiceId);
            throw;
        }
    }
}