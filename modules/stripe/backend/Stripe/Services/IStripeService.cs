using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Api.Abstractions;

namespace Stripe.Services;

public interface IStripeService
{
    Task<List<Endpoints.Subscriptions.GetUserSubscriptions.SubscriptionDto>> GetSubscriptionsForUserAsync(
        CancellationToken cancellationToken = default);
        
    Task<Endpoints.Subscriptions.GetUserSubscriptions.SubscriptionDto> UpdateSubscriptionTierAsync(
        string subscriptionId,
        ServerTierId serverTierId,
        CancellationToken cancellationToken = default);
        
    Task<Endpoints.Subscriptions.GetUserSubscriptions.SubscriptionDto> CancelSubscriptionAsync(
        string subscriptionId,
        bool cancelImmediately,
        CancellationToken cancellationToken = default);
        
    Task<List<PaymentMethodDto>> GetUserPaymentMethodsAsync(
        CancellationToken cancellationToken = default);
        
    Task<PaymentMethodDto> AddPaymentMethodAsync(
        string paymentMethodId,
        CancellationToken cancellationToken = default);
        
    Task<bool> RemovePaymentMethodAsync(
        string paymentMethodId,
        CancellationToken cancellationToken = default);
        
    Task<PaymentMethodDto> SetDefaultPaymentMethodAsync(
        string paymentMethodId,
        CancellationToken cancellationToken = default);
}

public class PaymentMethodDto
{
    public string Id { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty; // card, bank_account, etc.
    public string Brand { get; set; } = string.Empty; // Visa, Mastercard, etc.
    public string Last4 { get; set; } = string.Empty;
    public int ExpiryMonth { get; set; }
    public int ExpiryYear { get; set; }
    public bool IsDefault { get; set; }
}
