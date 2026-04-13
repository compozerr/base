using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Stripe.Endpoints.PaymentMethods.CloneFromPersonal;

public static class CloneFromPersonalRoute
{
    public static RouteGroupBuilder AddCloneFromPersonalRoute(this RouteGroupBuilder group)
    {
        group.MapPost("clone-from-personal", async (IMediator mediator) =>
        {
            var response = await mediator.Send(new CloneFromPersonalCommand());
            return Results.Ok(response);
        })
        .WithName("PostStripePaymentMethodsCloneFromPersonal")
        .Produces<CloneFromPersonalResponse>();

        return group;
    }
}
