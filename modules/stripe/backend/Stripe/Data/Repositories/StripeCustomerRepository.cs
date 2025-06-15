using Database.Repositories;
using Microsoft.EntityFrameworkCore;
using Stripe.Abstractions;
using Stripe.Data.Models;

namespace Stripe.Data.Repositories;

public interface IStripeCustomerRepository : IGenericRepository<StripeCustomer, StripeCustomerId, StripeDbContext>
{
    Task<string?> GetStripeCustomerIdByInternalId(string internalId);
    Task SetStripeCustomerIdAsync(string internalId, string customerId);
}

public sealed class StripeCustomerRepository(
    StripeDbContext context) : GenericRepository<StripeCustomer, StripeCustomerId, StripeDbContext>(context), IStripeCustomerRepository
{
    public Task<string?> GetStripeCustomerIdByInternalId(string internalId)
    {
        return Query().Where(x => x.InternalId == internalId)
                      .Select(x => x.StripeCustomerId)
                      .FirstOrDefaultAsync();
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