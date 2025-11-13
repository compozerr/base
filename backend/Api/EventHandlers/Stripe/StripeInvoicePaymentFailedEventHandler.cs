using Auth.Abstractions;
using Auth.Repositories;
using Core.Abstractions;
using Core.Services;
using Mail;
using Mail.Services;
using Stripe.Data.Repositories;
using Stripe.Events;

namespace Api.EventHandlers.Stripe;

public class StripeInvoicePaymentFailedEventHandler(
    IMailService mailService,
    IUserRepository userRepository,
    IStripeCustomerRepository stripeCustomerRepository,
    IFrontendLocation frontendLocation) : IEventHandler<StripeInvoicePaymentFailedEvent>
{
    private readonly Serilog.ILogger _logger = Log.Logger.ForContext<StripeInvoicePaymentFailedEventHandler>();

    public async Task Handle(
        StripeInvoicePaymentFailedEvent notification,
        CancellationToken cancellationToken)
    {
        _logger.Warning("Processing failed invoice payment - Invoice: {InvoiceId}, Customer: {CustomerId}, Amount: {AmountDue}, Attempt: {AttemptCount}",
            notification.InvoiceId,
            notification.CustomerId,
            notification.AmountDue,
            notification.AttemptCount);

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

        var currency = notification.Currency.ToUpper();

        if (currency == "USD")
        {
            currency = "$";
        }
        else if (currency == "EUR")
        {
            currency = "€";
        }

        try
        {
            var mail = await ReactEmail.CreateAsync(
                new EmailAddress("no-reply@notifications.compozerr.com", "compozerr hosting"),
                [new EmailAddress(user.Email, user.Name)],
                "Action Required: Payment Failed for Your Compozerr Invoice",
                new Emails.MissingInvoicePaymentTemplate()
                {
                    DueDate = notification.DueDate.ToString("MMMM dd, yyyy"),
                    CompanyName = "compozerr hosting",
                    CustomerName = user.Name,
                    Currency = currency,
                    AmountDue = notification.AmountDue.ToString("F2"),
                    DaysOverdue = notification.DaysOverdue.ToString(),
                    PaymentLink = notification.PaymentLink,
                    DashboardLink = frontendLocation.GetFromPath("/projects").ToString(),
                    ContactLink = frontendLocation.GetFromPath("/contact").ToString(),
                    CompanyAddress = "Vilh. Bergsøes Vej 11, 8210 Aarhus V, Denmark"
                });

            await mailService.SendEmailAsync(mail);
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Error handling failed payment for invoice: {InvoiceId}", notification.InvoiceId);
            throw;
        }
    }
}
