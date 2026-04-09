using Database.Models;
using Stripe.Abstractions;

namespace Stripe.Data.Models;


public class StripeCustomer : BaseEntityWithId<StripeCustomerId>
{
    /// <summary>
    /// The internal ID — organization ID (as string) that this Stripe customer belongs to.
    /// </summary>
    public required string InternalId { get; set; }

    public required string StripeCustomerId { get; set; }
}