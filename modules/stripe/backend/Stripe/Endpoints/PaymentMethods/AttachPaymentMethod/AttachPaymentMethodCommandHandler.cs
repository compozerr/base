using System.Security.Claims;
using Core.Extensions;
using Core.MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Memory;
using Stripe.Services;

namespace Stripe.Endpoints.PaymentMethods.AttachPaymentMethod;

public class AttachPaymentMethodCommandHandler(
    IStripeService stripeService,
    IMemoryCache memoryCache,
    IHttpContextAccessor accessor) : ICommandHandler<AttachPaymentMethodCommand, AttachPaymentMethodResponse>
{
    public async Task<AttachPaymentMethodResponse> Handle(
        AttachPaymentMethodCommand request,
        CancellationToken cancellationToken)
    {
        var userId = accessor.HttpContext?.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId))
        {
            throw new UnauthorizedAccessException("User is not authenticated.");
        }

        var userPaymentMethods = await stripeService.GetUserPaymentMethodsAsync(cancellationToken);

        PaymentMethodDto? paymentMethod = null;

        if (!userPaymentMethods.Any(p => p.Id == request.PaymentMethodId))
        {
            paymentMethod = await stripeService.AddPaymentMethodAsync(
                request.PaymentMethodId,
                cancellationToken);
        }

        //Remove old payment methods if the user already has one
        await userPaymentMethods.Where(p => p.Id != request.PaymentMethodId)
                                .ApplyAsync(
                                    (p) => stripeService.RemovePaymentMethodAsync(p.Id, cancellationToken));

        memoryCache.Remove($"UserPaymentMethods-{userId}");
        return new AttachPaymentMethodResponse(paymentMethod ?? userPaymentMethods.Single(p => p.Id == request.PaymentMethodId));
    }
}
