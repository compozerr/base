using Api.EventHandlers.Stripe.PaymentFailedActions.Core;
using Auth.Models;
using Core.Services;
using Mail;
using Mail.Services;
using Stripe.Events;

namespace Api.EventHandlers.Stripe.PaymentFailedActions;

public sealed class FirstWarningMail_PaymentFailedAction(
    PaymentFailedEvent @event,
    User user,
    IServiceProvider serviceProvider
) : BasePaymentFailedAction(@event, user, serviceProvider)
{
    private readonly Serilog.ILogger _logger = Log.Logger.ForContext<FirstWarningMail_PaymentFailedAction>();

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
                "Payment Reminder: Invoice Overdue - 4 Days Until Project Termination",
                new Emails.FailedInvoiceFirstWarningTemplate()
                {
                    CustomerName = User.Name,
                    AmountDue = Event.AmountDue.ToString("F2"),
                    Currency = Event.Currency.ToUpper(),
                    DueDate = Event.DueDate.ToString("MMMM dd, yyyy"),
                    DaysOverdue = Event.DaysOverdue.ToString(),
                    DaysUntilTermination = "4",
                    PaymentLink = Event.PaymentLink,
                    DashboardLink = frontendLocation.GetFromPath("/projects").ToString(),
                    ContactLink = frontendLocation.GetFromPath("/contact").ToString(),
                    CompanyName = "compozerr hosting",
                    CompanyAddress = "Vilh. Bergsøes Vej 11, 8210 Aarhus V, Denmark"
                });

            await mailService.SendEmailAsync(mail);

            _logger.Information("Successfully sent failed invoice first warning email for invoice: {InvoiceId}", Event.InvoiceId);
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Error sending failed invoice first warning email for invoice: {InvoiceId}", Event.InvoiceId);
            throw;
        }
    }
}
