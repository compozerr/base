using Microsoft.Extensions.Options;
using Serilog;
using Stripe.Options;

namespace Stripe.Services;

public interface IInvoicesService
{
    Task<List<InvoiceDto>> GetInvoicesForCurrentCustomerAsync(CancellationToken cancellationToken);
}

public class InvoicesService(
    IOptions<StripeOptions> options,
    ICurrentStripeCustomerIdAccessor currentStripeCustomerIdAccessor) : IInvoicesService
{
    private readonly StripeClient _stripeClient = new(options.Value.ApiKey);

    public async Task<List<InvoiceDto>> GetInvoicesForCurrentCustomerAsync(CancellationToken cancellationToken)
    {
        var stripeCustomerId = await currentStripeCustomerIdAccessor.GetOrCreateStripeCustomerId();

        try
        {
            var service = new InvoiceService(_stripeClient);
            var listOptions = new InvoiceListOptions
            {
                Customer = stripeCustomerId,
                Limit = 100,
            };

            var invoices = await service.ListAsync(
                listOptions,
                null,
                cancellationToken);

            return [.. invoices
                .Where(i => i.Status != "draft")
                .Select(InvoiceDto.FromInvoice)];
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error retrieving invoices for customer {CustomerId}", stripeCustomerId);
        }

        return [];
    }
}
