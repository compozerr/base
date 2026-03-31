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
    }
}
