using Stripe.Services;

namespace Stripe.Endpoints.PaymentMethods.CloneFromPersonal;

public sealed record CloneFromPersonalResponse(PaymentMethodDto PaymentMethod);
