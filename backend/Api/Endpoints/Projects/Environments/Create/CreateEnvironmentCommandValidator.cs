using Api.Abstractions;
using Api.Data.Extensions;
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

        RuleFor(x => x.Branch)
            .NotEmpty()
            .MaximumLength(255)
            .WithMessage("Branch name is required.");

        RuleFor(x => x.Tier).Must(tier =>
        {
            return ServerTiers.All.Select(x => x.Id.Value).Contains(tier);
        }).WithMessage("Invalid tier specified.")
          .NotEmpty().WithMessage("Tier cannot be empty.")
          .NotNull().WithMessage("Tier cannot be null.");
    }
}
