using Core.MediatR;

namespace Stripe.Endpoints.PaymentMethods.RemovePaymentMethod;

public sealed record RemovePaymentMethodCommand(
    string PaymentMethodId) : ICommand<RemovePaymentMethodResponse>;

public sealed record RemovePaymentMethodResponse(
    bool Success);
