using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Stripe.Endpoints.PaymentMethods.GetPersonalOrgHasPaymentMethod;

public static class GetPersonalOrgHasPaymentMethodRoute
{
    public static RouteGroupBuilder AddGetPersonalOrgHasPaymentMethodRoute(this RouteGroupBuilder group)
    {
        group.MapGet("personal-has-payment-method", async (IMediator mediator) =>
        {
            var response = await mediator.Send(new GetPersonalOrgHasPaymentMethodCommand());
            return Results.Ok(response);
        })
        .WithName("GetStripePaymentMethodsPersonalHasPaymentMethod")
        .Produces<GetPersonalOrgHasPaymentMethodResponse>();

        return group;
    }
}
