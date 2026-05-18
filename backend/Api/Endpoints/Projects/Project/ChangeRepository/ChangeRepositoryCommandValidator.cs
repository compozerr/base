using Api.Data.Extensions;
using FluentValidation;

namespace Api.Endpoints.Projects.Project.ChangeRepository;

public sealed class ChangeRepositoryCommandValidator : AbstractValidator<ChangeRepositoryCommand>
{
    public ChangeRepositoryCommandValidator(IServiceScopeFactory scopeFactory)
    {
        RuleFor(x => x.ProjectId).MustBeOwnedByCallerAsync(scopeFactory);

        RuleFor(x => x.RepoFullName)
            .NotEmpty()
            .Matches(@"^[^/\s]+/[^/\s]+$")
            .WithMessage("RepoFullName must be in the form 'owner/repo'.");
    }
}
