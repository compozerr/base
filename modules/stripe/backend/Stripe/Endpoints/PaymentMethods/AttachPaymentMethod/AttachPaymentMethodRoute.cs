using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using MediatR;
using System.Threading.Tasks;

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
        => await mediator.Send(new AttachPaymentMethodCommand(request.UserId, request.PaymentMethodId));
}

public record AttachPaymentMethodRequest(string UserId, string PaymentMethodId);
