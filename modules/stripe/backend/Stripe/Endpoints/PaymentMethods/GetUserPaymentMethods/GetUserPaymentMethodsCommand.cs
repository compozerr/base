using Core.MediatR;
using Stripe.Services;

namespace Stripe.Endpoints.PaymentMethods.GetUserPaymentMethods;

public sealed record GetUserPaymentMethodsCommand() : ICommand<GetUserPaymentMethodsResponse>;

public sealed record GetUserPaymentMethodsResponse(
    List<PaymentMethodDto> PaymentMethods);
