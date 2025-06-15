using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Stripe.Endpoints.PaymentMethods.GetUserPaymentMethods;
using Stripe.Endpoints.PaymentMethods.AttachPaymentMethod;

namespace Stripe.Endpoints.PaymentMethods;

public static class PaymentMethodsGroup
{
    public const string Route = "payment-methods";

    public static RouteGroupBuilder AddPaymentMethodsGroup(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup(Route);

        group.AddGetUserPaymentMethodsRoute();
        group.AddAttachPaymentMethodRoute();
        
        return group;
    }
}
