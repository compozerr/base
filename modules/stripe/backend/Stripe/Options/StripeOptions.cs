using System.ComponentModel.DataAnnotations;

namespace Stripe.Options;

public sealed class StripeOptions
{
    [Required]
    public required string ApiKey { get; init; }
}