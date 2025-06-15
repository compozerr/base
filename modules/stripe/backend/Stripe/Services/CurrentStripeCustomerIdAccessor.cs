
using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Stripe.Data.Repositories;

namespace Stripe.Services;

public sealed class CurrentStripeCustomerIdAccessor(
    IHttpContextAccessor httpContextAccessor,
    IStripeCustomerRepository stripeCustomerRepository) : ICurrentStripeCustomerIdAccessor
{
    public Task<string?> GetStripeCustomerId()
    {
        var userId = GetUserIdFromContext();

        return stripeCustomerRepository.GetStripeCustomerIdByInternalId(userId);
    }

    public Task SetStripeCustomerIdAsync(string customerId)
    {
        var userId = GetUserIdFromContext();

        return stripeCustomerRepository.SetStripeCustomerIdAsync(userId, customerId);
    }

    private string GetUserIdFromContext()
    {
        return httpContextAccessor.HttpContext
                                  ?.User
                                  ?.FindFirst(ClaimTypes.NameIdentifier)
                                  ?.Value ?? throw new InvalidOperationException("User is not authenticated.");
    }
}
