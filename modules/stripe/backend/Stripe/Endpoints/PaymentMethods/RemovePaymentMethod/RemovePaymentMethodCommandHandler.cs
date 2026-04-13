using System.Security.Claims;
using Core.MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Memory;
using Organizations.Services;
using Stripe.Services;

namespace Stripe.Endpoints.PaymentMethods.RemovePaymentMethod;

public class RemovePaymentMethodCommandHandler(
    IPaymentMethodsService paymentMethodsService,
    IMemoryCache memoryCache,
    IHttpContextAccessor accessor,
    IOrganizationContextAccessor organizationContextAccessor) : ICommandHandler<RemovePaymentMethodCommand, RemovePaymentMethodResponse>
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

        var result = await paymentMethodsService.RemovePaymentMethodAsync(
            request.PaymentMethodId,
            cancellationToken);

        var orgId = await organizationContextAccessor.GetCurrentOrganizationIdAsync();
        memoryCache.Remove($"PaymentMethods-{orgId.Value}");

        return new RemovePaymentMethodResponse(result);
    }
}
