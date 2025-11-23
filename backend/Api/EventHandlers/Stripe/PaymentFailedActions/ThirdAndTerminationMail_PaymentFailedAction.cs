using Api.EventHandlers.Stripe.PaymentFailedActions.Core;
using Auth.Models;
using Core.Services;
using Mail;
using Mail.Services;
using Stripe.Events;
using Stripe.Helpers;

namespace Api.EventHandlers.Stripe.PaymentFailedActions;

public sealed class ThirdAndTerminationMail_PaymentFailedAction(
    StripeInvoicePaymentFailedEvent @event,
    User user,
    IServiceProvider serviceProvider
) : BasePaymentFailedAction(@event, user, serviceProvider)
{
    private readonly Serilog.ILogger _logger = Log.Logger.ForContext<ThirdAndTerminationMail_PaymentFailedAction>();
    public override async Task ExecuteAsync(CancellationToken cancellationToken = default)
    {
        using var scope = ServiceProvider.CreateScope();
        var mailService = scope.ServiceProvider.GetRequiredService<IMailService>();
        var frontendLocation = scope.ServiceProvider.GetRequiredService<IFrontendLocation>();

        try
        {
            var mail = await ReactEmail.CreateAsync(
                new EmailAddress("no-reply@notifications.compozerr.com", "compozerr hosting"),
                [new EmailAddress(User.Email, User.Name)],
                "Important Notice: Your Compozerr Project Has Been Terminated",
                new Emails.ProjectHasBeenTerminatedTemplate()
                {
                    CompanyName = "compozerr hosting",
                    Reason = "Due to lack of payment, we have regrettably terminated your project associated with this account.",
                    CustomerName = User.Name,
                    DashboardLink = frontendLocation.GetFromPath("/projects").ToString(),
                    ContactLink = frontendLocation.GetFromPath("/contact").ToString(),
                    CompanyAddress = "Vilh. Bergsøes Vej 11, 8210 Aarhus V, Denmark"
                });

            await mailService.SendEmailAsync(mail);

            _logger.Information("Successfully sent project termination notification for invoice: {InvoiceId}", Event.InvoiceId);
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Error handling project termination for invoice: {InvoiceId}", Event.InvoiceId);
            throw;
        }
    }
}