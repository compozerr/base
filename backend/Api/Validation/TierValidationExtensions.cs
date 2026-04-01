using Api.Abstractions;
using FluentValidation;

namespace Api.Validation;

public static class TierValidationExtensions
{
    public static IRuleBuilderOptions<T, string> MustBeValidServerTier<T>(
        this IRuleBuilder<T, string> ruleBuilder)
    {
        return ruleBuilder
            .NotEmpty().WithMessage("Tier cannot be empty.")
            .NotNull().WithMessage("Tier cannot be null.")
            .Must(tier => ServerTiers.All.Select(x => x.Id.Value).Contains(tier))
            .WithMessage("Invalid tier specified. Valid tiers: " + string.Join(", ", ServerTiers.All.Select(x => x.Id.Value)));
    }
}
