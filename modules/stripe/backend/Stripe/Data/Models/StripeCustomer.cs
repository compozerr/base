using Database.Models;
using Stripe.Abstractions;

namespace Stripe.Data.Models;


public class StripeCustomer : BaseEntityWithId<StripeCustomerId>
{
    /// <summary>
    /// The internal ID of the user in the database.
    /// </summary>
    public required string InternalId { get; set; }

    public required string StripeCustomerId { get; set; }
}