using Microsoft.Extensions.Options;
using Serilog;
using Stripe.Options;

namespace Stripe.Services;

public sealed record InvoicePage(List<InvoiceDto> Invoices, bool HasMore, string? NextCursor);

public interface IInvoicesService
{
    Task<InvoicePage> GetInvoicesForCurrentCustomerAsync(int limit, string? startingAfter, CancellationToken cancellationToken);
}

public class InvoicesService(
    IOptions<StripeOptions> options,
    ICurrentStripeCustomerIdAccessor currentStripeCustomerIdAccessor) : IInvoicesService
{
    private readonly StripeClient _stripeClient = new(options.Value.ApiKey);

    public async Task<InvoicePage> GetInvoicesForCurrentCustomerAsync(int limit, string? startingAfter, CancellationToken cancellationToken)
    {
        var stripeCustomerId = await currentStripeCustomerIdAccessor.GetOrCreateStripeCustomerId();

        try
        {
            var service = new InvoiceService(_stripeClient);
            var collected = new List<global::Stripe.Invoice>();
            string? lastScannedId = null;
            var cursor = startingAfter;
            var stripeHasMore = true;

            // Iterate until we've filled the requested page with visible invoices (paid/open)
            // or Stripe has no more to give. Without this loop, a page of filtered-out statuses
            // (void/draft/uncollectible) would return empty while reporting hasMore=true, which
            // traps the infinite-scroll consumer in a fetch loop.
            while (collected.Count < limit && stripeHasMore)
            {
                var page = await service.ListAsync(
                    new InvoiceListOptions
                    {
                        Customer = stripeCustomerId,
                        Limit = 100,
                        StartingAfter = cursor,
                    },
                    null,
                    cancellationToken);

                var brokeEarly = false;
                foreach (var invoice in page.Data)
                {
                    lastScannedId = invoice.Id;
                    if (invoice.Status is "paid" or "open")
                    {
                        collected.Add(invoice);
                        if (collected.Count >= limit)
                        {
                            brokeEarly = true;
                            break;
                        }
                    }
                }

                // If we broke mid-batch, items between lastScannedId and the end of this batch
                // are still accessible via the cursor. Only trust page.HasMore when we scanned
                // the whole batch.
                stripeHasMore = brokeEarly || page.HasMore;
                cursor = lastScannedId ?? cursor;
            }

            var dtos = collected.Select(InvoiceDto.FromInvoice).ToList();
            var hasMore = stripeHasMore && collected.Count >= limit;
            var nextCursor = hasMore ? lastScannedId : null;

            return new InvoicePage(dtos, hasMore, nextCursor);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error retrieving invoices for customer {CustomerId}", stripeCustomerId);
        }

        return new InvoicePage([], false, null);
    }
}
