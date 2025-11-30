using Api.EventHandlers.Stripe.PaymentMethodMissingActions.Core;
using Auth.Models;
using Core.Services;
using Mail;
using Mail.Services;
using Stripe.Events;

namespace Api.EventHandlers.Stripe.PaymentMethodMissingActions;

public sealed class SecondWarningMail_PaymentMethodMissingAction(
    PaymentMethodMissingEvent @event,
    User user,
    IServiceProvider serviceProvider
) : BasePaymentMethodMissingAction(@event, user, serviceProvider)
{
    private readonly Serilog.ILogger _logger = Log.Logger.ForContext<SecondWarningMail_PaymentMethodMissingAction>();
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
                "Urgent: Add Payment Method to Prevent Service Termination",
                new Emails.MissingPaymentMethodTemplate()
                {
                    CompanyName = "compozerr hosting",
                    Description = "This is a second reminder to add a valid payment method to your account. Your service will be terminated soon if no payment method is added.",
                    CustomerName = User.Name,
                    AddPaymentMethodUrl = frontendLocation.GetFromPath("settings?addPaymentMethod=true").ToString(),
                    DashboardLink = frontendLocation.GetFromPath("/projects").ToString(),
                    ContactLink = frontendLocation.GetFromPath("/contact").ToString(),
                    CompanyAddress = "Vilh. Bergsøes Vej 11, 8210 Aarhus V, Denmark"
                });

            await mailService.SendEmailAsync(mail);

            _logger.Information("Successfully sent second warning payment method missing notification for customer: {CustomerId}", Event.CustomerId);
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Error handling missing payment method for customer: {CustomerId}", Event.CustomerId);
            throw;
        }
    }
}