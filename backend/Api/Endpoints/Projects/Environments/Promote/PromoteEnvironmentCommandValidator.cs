using Api.Data.Extensions;
using FluentValidation;

namespace Api.Endpoints.Projects.Environments.Promote;

public sealed class PromoteEnvironmentCommandValidator : AbstractValidator<PromoteEnvironmentCommand>
{
    public PromoteEnvironmentCommandValidator(IServiceScopeFactory scopeFactory)
    {
        RuleFor(x => x.ProjectId)
            .MustBeOwnedByCallerAsync(scopeFactory);

        RuleFor(x => x.EnvironmentId)
            .NotEmpty()
            .WithMessage("Environment ID is required.");
    }
}
