using Api.Data.Extensions;
using Api.Validation;
using FluentValidation;

namespace Api.Endpoints.Projects.Environments.ChangeTier;

public sealed class ChangeEnvironmentTierCommandValidator : AbstractValidator<ChangeEnvironmentTierCommand>
{
    public ChangeEnvironmentTierCommandValidator(IServiceScopeFactory scopeFactory)
    {
        RuleFor(x => x.Tier).MustBeValidServerTier();

        RuleFor(x => x.ProjectId).MustBeOwnedByCallerAsync(scopeFactory);
    }
}
