using Core.MediatR;
using System.Collections.Generic;
using Stripe.Services;

namespace Stripe.Endpoints.PaymentMethods.GetUserPaymentMethods;

public sealed record GetUserPaymentMethodsCommand(
    string UserId) : ICommand<GetUserPaymentMethodsResponse>;

public sealed record GetUserPaymentMethodsResponse(
    List<PaymentMethodDto> PaymentMethods);
