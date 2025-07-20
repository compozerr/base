using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Stripe.Endpoints.PaymentMethods.GetUserPaymentMethods;
using Stripe.Endpoints.PaymentMethods.AttachPaymentMethod;
using Stripe.Endpoints.PaymentMethods.SetDefaultPaymentMethod;
using Stripe.Endpoints.PaymentMethods.RemovePaymentMethod;
using Stripe.Endpoints.PaymentMethods.CreateSetupIntent;

namespace Stripe.Endpoints.PaymentMethods;

public static class PaymentMethodsGroup
{
    public const string Route = "payment-methods";

    public static RouteGroupBuilder AddPaymentMethodsGroup(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup(Route);

        group.AddGetUserPaymentMethodsRoute();
        group.AddAttachPaymentMethodRoute();
        group.AddSetDefaultPaymentMethodRoute();
        group.AddRemovePaymentMethodRoute();
        group.AddCreateSetupIntentRoute();
        
        return group;
    }
}
