using System.Security.Claims;
using Core.MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Memory;
using Stripe.Services;

namespace Stripe.Endpoints.PaymentMethods.GetUserPaymentMethods;

public class GetUserPaymentMethodsCommandHandler(
    IStripeService stripeService,
    IMemoryCache memoryCache,
    IHttpContextAccessor accessor) : ICommandHandler<GetUserPaymentMethodsCommand, GetUserPaymentMethodsResponse>
{
    public async Task<GetUserPaymentMethodsResponse> Handle(
        GetUserPaymentMethodsCommand request,
        CancellationToken cancellationToken)
    {
        var userId = accessor.HttpContext?.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId))
        {
            throw new UnauthorizedAccessException("User is not authenticated.");
        }

        var paymentMethods = await memoryCache.GetOrCreateAsync($"UserPaymentMethods-{userId}", async entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5);

            return await stripeService.GetUserPaymentMethodsAsync(cancellationToken);
        });

        return new GetUserPaymentMethodsResponse(paymentMethods ?? []);
    }
}
