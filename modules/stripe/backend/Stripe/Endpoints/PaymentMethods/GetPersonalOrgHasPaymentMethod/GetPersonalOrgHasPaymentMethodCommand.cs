using Core.MediatR;

namespace Stripe.Endpoints.PaymentMethods.GetPersonalOrgHasPaymentMethod;

public sealed record GetPersonalOrgHasPaymentMethodCommand : ICommand<GetPersonalOrgHasPaymentMethodResponse>;

public sealed record GetPersonalOrgHasPaymentMethodResponse(bool HasPaymentMethod, string? Last4, string? Brand);
