using System.Security.Claims;
using Core.MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Memory;
using Stripe.Services;

namespace Stripe.Endpoints.PaymentMethods.RemovePaymentMethod;

public class RemovePaymentMethodCommandHandler(
    IStripeService stripeService,
    IMemoryCache memoryCache,
    IHttpContextAccessor accessor) : ICommandHandler<RemovePaymentMethodCommand, RemovePaymentMethodResponse>
{
    public async Task<RemovePaymentMethodResponse> Handle(
        RemovePaymentMethodCommand request,
        CancellationToken cancellationToken)
    {
        var userId = accessor.HttpContext?.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId))
        {
            throw new UnauthorizedAccessException("User is not authenticated.");
        }

        var result = await stripeService.RemovePaymentMethodAsync(
            request.PaymentMethodId,
            cancellationToken);

        memoryCache.Remove($"UserPaymentMethods-{userId}");

        return new RemovePaymentMethodResponse(result);
    }
}
