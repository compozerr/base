using Database.Repositories;
using Microsoft.EntityFrameworkCore;
using Stripe.Abstractions;
using Stripe.Data.Models;

namespace Stripe.Data.Repositories;

public interface IStripeCustomerRepository : IGenericRepository<StripeCustomer, StripeCustomerId, StripeDbContext>
{
    Task<string?> GetStripeCustomerIdByInternalIdAsync(string internalId, CancellationToken cancellationToken = default);
    Task<string?> GetInternalIdByStripeCustomerIdAsync(string customerId, CancellationToken cancellationToken = default);
    Task SetStripeCustomerIdAsync(string internalId, string customerId);
}

public sealed class StripeCustomerRepository(
    StripeDbContext context) : GenericRepository<StripeCustomer, StripeCustomerId, StripeDbContext>(context), IStripeCustomerRepository
{
    public Task<string?> GetInternalIdByStripeCustomerIdAsync(string customerId, CancellationToken cancellationToken = default)
    {
        return Query().Where(x => x.StripeCustomerId == customerId)
            .Select(x => x.InternalId)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public Task<string?> GetStripeCustomerIdByInternalIdAsync(string internalId, CancellationToken cancellationToken = default)
    {
        return Query().Where(x => x.InternalId == internalId)
                      .Select(x => x.StripeCustomerId)
                      .FirstOrDefaultAsync(cancellationToken);
    }

    public Task SetStripeCustomerIdAsync(string internalId, string customerId)
    {
        if (string.IsNullOrWhiteSpace(internalId))
            throw new ArgumentException("Internal ID cannot be null or empty.", nameof(internalId));

        if (string.IsNullOrWhiteSpace(customerId))
            throw new ArgumentException("Customer ID cannot be null or empty.", nameof(customerId));

        var existingCustomer = Query().FirstOrDefault(x => x.InternalId == internalId);
        if (existingCustomer == null)
        {
            existingCustomer = new StripeCustomer
            {
                InternalId = internalId,
                StripeCustomerId = customerId
            };
            return AddAsync(existingCustomer);
        }

        existingCustomer.StripeCustomerId = customerId;

        return UpdateAsync(existingCustomer);
    }
}