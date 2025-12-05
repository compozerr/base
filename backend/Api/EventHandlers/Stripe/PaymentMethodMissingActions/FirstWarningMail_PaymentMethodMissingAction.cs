using Api.EventHandlers.Stripe.PaymentMethodMissingActions.Core;
using Auth.Models;
using Core.Services;
using Mail;
using Mail.Services;
using Stripe.Events;

namespace Api.EventHandlers.Stripe.PaymentMethodMissingActions;

public sealed class FirstWarningMail_PaymentMethodMissingAction(
    PaymentMethodMissingEvent @event,
    User user,
    IServiceProvider serviceProvider
) : BasePaymentMethodMissingAction(@event, user, serviceProvider)
{
    private readonly Serilog.ILogger _logger = Log.Logger.ForContext<FirstWarningMail_PaymentMethodMissingAction>();
    public override async Task ExecuteAsync(CancellationToken cancellationToken = default)
    {
        using var scope = ServiceProvider.CreateScope();
        var mailService = scope.ServiceProvider.GetRequiredService<IMailService>();
        var frontendLocation = scope.ServiceProvider.GetRequiredService<IFrontendLocation>();

        try
        {
            var remainingDays = GetRemainingDaysUntilTermination();
            var mail = await ReactEmail.CreateAsync(
                new EmailAddress("hosting-noreply@notifications.compozerr.com", "compozerr hosting"),
                [new EmailAddress(User.Email, User.Name)],
                "Action Required: Add Payment Method to Continue Your Service",
                new Emails.MissingPaymentMethodTemplate()
                {
                    CompanyName = "compozerr hosting",
                    Description = $"To avoid service termination, please add a valid payment method to your account. Your projects will be terminated in {remainingDays} day{(remainingDays != 1 ? "s" : "")} if no payment method is added.",
                    CustomerName = User.Name,
                    AddPaymentMethodUrl = frontendLocation.GetFromPath("settings?addPaymentMethod=true").ToString(),
                    DashboardLink = frontendLocation.GetFromPath("/projects").ToString(),
                    ContactLink = frontendLocation.GetFromPath("/contact").ToString(),
                    CompanyAddress = "Vilh. Bergsøes Vej 11, 8210 Aarhus V, Denmark"
                });

            await mailService.SendEmailAsync(mail);

            _logger.Information("Successfully sent first warning payment method missing notification for customer: {CustomerId}", Event.CustomerId);
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Error handling missing payment method for customer: {CustomerId}", Event.CustomerId);
            throw;
        }
    }
}