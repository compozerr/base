using Api.Data.Repositories;
using Api.EventHandlers.Stripe.PaymentFailedActions.Core;
using Auth.Models;
using Core.Services;
using Mail;
using Mail.Services;
using Stripe.Events;
using Stripe.Helpers;

namespace Api.EventHandlers.Stripe.PaymentFailedActions;

public sealed class TerminateProject_PaymentFailedAction(
    StripeInvoicePaymentFailedEvent @event,
    User user,
    IServiceProvider serviceProvider
) : BasePaymentFailedAction(@event, user, serviceProvider)
{
    private readonly Serilog.ILogger _logger = Log.Logger.ForContext<TerminateProject_PaymentFailedAction>();
    public override async Task ExecuteAsync(CancellationToken cancellationToken = default)
    {
        using var scope = ServiceProvider.CreateScope();
        var projectRepository = scope.ServiceProvider.GetRequiredService<IProjectRepository>();

        try
        {
           
            _logger.Information("Successfully sent terminated project for invoice: {InvoiceId}", Event.InvoiceId);
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Error handling terminated project for invoice: {InvoiceId}", Event.InvoiceId);
            throw;
        }
    }
}