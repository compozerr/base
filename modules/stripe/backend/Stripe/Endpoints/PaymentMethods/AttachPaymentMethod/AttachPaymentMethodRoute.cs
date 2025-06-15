using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using MediatR;

namespace Stripe.Endpoints.PaymentMethods.AttachPaymentMethod;

public static class AttachPaymentMethodRoute
{
    public const string Route = "attach";

    public static RouteHandlerBuilder AddAttachPaymentMethodRoute(this IEndpointRouteBuilder app)
    {
        return app.MapPost(Route, ExecuteAsync);
    }

    public static async Task<AttachPaymentMethodResponse> ExecuteAsync(
        AttachPaymentMethodRequest request,
        IMediator mediator)
        => await mediator.Send(new AttachPaymentMethodCommand(request.PaymentMethodId));
}

public record AttachPaymentMethodRequest(string PaymentMethodId);
