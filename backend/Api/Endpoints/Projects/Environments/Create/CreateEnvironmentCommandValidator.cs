using Api.Data.Extensions;
using Api.Validation;
using FluentValidation;

namespace Api.Endpoints.Projects.Environments.Create;

public sealed class CreateEnvironmentCommandValidator : AbstractValidator<CreateEnvironmentCommand>
{
    public CreateEnvironmentCommandValidator(IServiceScopeFactory scopeFactory)
    {
        RuleFor(x => x.ProjectId)
            .MustBeOwnedByCallerAsync(scopeFactory);

        RuleFor(x => x.Name)
            .NotEmpty()
            .MaximumLength(255)
            .WithMessage("Environment name is required.");

        When(x => x.Branch is not null, () =>
        {
            RuleFor(x => x.Branch)
                .NotEmpty()
                .MaximumLength(255);
        });

        RuleFor(x => x.Tier).MustBeValidServerTier();
    }
}
