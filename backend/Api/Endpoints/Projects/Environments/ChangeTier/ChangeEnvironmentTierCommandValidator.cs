using Api.Abstractions;
using Api.Data.Extensions;
using FluentValidation;

namespace Api.Endpoints.Projects.Environments.ChangeTier;

public sealed class ChangeEnvironmentTierCommandValidator : AbstractValidator<ChangeEnvironmentTierCommand>
{
    public ChangeEnvironmentTierCommandValidator(IServiceScopeFactory scopeFactory)
    {
        RuleFor(x => x.Tier).Must(tier =>
        {
            return ServerTiers.All.Select(x => x.Id.Value).Contains(tier);
        }).WithMessage("Invalid tier specified.")
          .NotEmpty().WithMessage("Tier cannot be empty.")
          .NotNull().WithMessage("Tier cannot be null.");

        RuleFor(x => x.ProjectId).MustBeOwnedByCallerAsync(scopeFactory);
    }
}
