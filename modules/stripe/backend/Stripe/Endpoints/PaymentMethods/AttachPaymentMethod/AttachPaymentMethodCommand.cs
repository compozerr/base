using Core.MediatR;
using Stripe.Services;

namespace Stripe.Endpoints.PaymentMethods.AttachPaymentMethod;

public sealed record AttachPaymentMethodCommand(
    string UserId,
    string PaymentMethodId) : ICommand<AttachPaymentMethodResponse>;

public sealed record AttachPaymentMethodResponse(
    PaymentMethodDto PaymentMethod);
