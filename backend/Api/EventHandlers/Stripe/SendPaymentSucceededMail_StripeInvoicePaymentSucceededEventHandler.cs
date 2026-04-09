using Core.Abstractions;
using Core.Services;
using Mail;
using Mail.Services;
using Stripe.Events;
using Stripe.Helpers;
using Stripe.Services;

namespace Api.EventHandlers.Stripe;

public class SendPaymentSucceededMail_StripeInvoicePaymentSucceededEventHandler(
    IMailService mailService,
    IStripeOrgResolver stripeOrgResolver,
    IFrontendLocation frontendLocation) : IEventHandler<StripeInvoicePaymentSucceededEvent>
{
    private readonly Serilog.ILogger _logger = Log.Logger.ForContext<SendPaymentSucceededMail_StripeInvoicePaymentSucceededEventHandler>();

    public async Task Handle(
        StripeInvoicePaymentSucceededEvent notification,
        CancellationToken cancellationToken)
    {
        var resolved = await stripeOrgResolver.ResolveOrgAndOwnerByStripeCustomerIdAsync(notification.CustomerId, cancellationToken);
        if (resolved is null)
        {
            _logger.ForContext(nameof(notification), notification, true)
                   .Error("Cannot send payment succeeded notification for Invoice: {InvoiceId}, Customer: {CustomerId}",
                notification.InvoiceId,
                notification.CustomerId);
            return;
        }

        var user = resolved.Owner;
        var currencySymbol = CurrencyHelper.GetSymbol(notification.Currency);

        try
        {
            var mail = await ReactEmail.CreateAsync(
                new EmailAddress("hosting-noreply@notifications.compozerr.com", "Compozerr Hosting"),
                [new EmailAddress(user.Email, user.Name)],
                "Your Compozerr Invoice Payment Succeeded",
                new Emails.InvoicePaymentSucceededTemplate()
                {
                    PaymentDate = notification.PaidAt.ToString("MMMM dd, yyyy"),
                    CompanyName = "compozerr hosting",
                    CustomerName = user.Name,
                    Currency = currencySymbol,
                    AmountPaid = notification.AmountPaid.ToString("F2"),
                    InvoiceLink = string.IsNullOrEmpty(notification.InvoiceLink) ? frontendLocation.GetFromPath("/settings").ToString() : notification.InvoiceLink,
                    DashboardLink = frontendLocation.GetFromPath("/projects").ToString(),
                    ContactLink = frontendLocation.GetFromPath("/contact").ToString(),
                    CompanyAddress = "Vilh. Bergsøes Vej 11, 8210 Aarhus V, Denmark",
                });

            await mailService.SendEmailAsync(mail);

            _logger.Information("Successfully sent payment succeeded notification for invoice: {InvoiceId}", notification.InvoiceId);
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Error handling succeeded payment for invoice: {InvoiceId}", notification.InvoiceId);
            throw;
        }
    }
}
