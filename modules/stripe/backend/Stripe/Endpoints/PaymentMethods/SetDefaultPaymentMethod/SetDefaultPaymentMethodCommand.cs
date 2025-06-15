using Core.MediatR;
using Stripe.Services;

namespace Stripe.Endpoints.PaymentMethods.SetDefaultPaymentMethod;

public sealed record SetDefaultPaymentMethodCommand(
    string UserId,
    string PaymentMethodId) : ICommand<SetDefaultPaymentMethodResponse>;

public sealed record SetDefaultPaymentMethodResponse(
    PaymentMethodDto PaymentMethod);
