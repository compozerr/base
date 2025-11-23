using Auth.Abstractions;
using Auth.Repositories;
using Core.Abstractions;
using Core.Services;
using Mail;
using Mail.Services;
using Stripe.Data.Repositories;
using Stripe.Events;
using Stripe.Helpers;

namespace Api.EventHandlers.Stripe;

public class SendPaymentSucceededMail_StripeInvoicePaymentSucceededEventHandler(
    IMailService mailService,
    IUserRepository userRepository,
    IStripeCustomerRepository stripeCustomerRepository,
    IFrontendLocation frontendLocation) : IEventHandler<StripeInvoicePaymentSucceededEvent>
{
    private readonly Serilog.ILogger _logger = Log.Logger.ForContext<SendPaymentSucceededMail_StripeInvoicePaymentSucceededEventHandler>();

    public async Task Handle(
        StripeInvoicePaymentSucceededEvent notification,
        CancellationToken cancellationToken)
    {
        var userId = await stripeCustomerRepository.GetInternalIdByStripeCustomerIdAsync(
            notification.CustomerId,
            cancellationToken);

        if (!UserId.TryParse(userId, out var parsedUserId) || userId == null)
        {
            _logger.ForContext(nameof(notification), notification, true)
                   .Error("No internal user id found for Stripe Customer ID: {CustomerId}. Cannot send failed payment notification for Invoice: {InvoiceId}",
                notification.CustomerId,
                notification.InvoiceId);
            return;
        }

        var user = await userRepository.GetByIdAsync(
            parsedUserId,
            cancellationToken);

        if (user == null)
        {
            _logger.ForContext(nameof(notification), notification, true)
                   .Error("User with ID: {UserId} not found. Cannot send failed payment notification for Invoice: {InvoiceId}",
                parsedUserId,
                notification.InvoiceId);
            return;
        }

        var currencySymbol = CurrencyHelper.GetSymbol(notification.Currency);

        try
        {
            var mail = await ReactEmail.CreateAsync(
                new EmailAddress("no-reply@notifications.compozerr.com", "compozerr hosting"),
                [new EmailAddress(user.Email, user.Name)],
                "Action Required: Payment Failed for Your Compozerr Invoice",
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