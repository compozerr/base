using Api.Abstractions;
using Api.Data.Extensions;
using FluentValidation;

namespace Api.Endpoints.Projects.Deployments.DeployProject;

public sealed class DeployProjectCommandValidator : AbstractValidator<DeployProjectCommand>
{
    public DeployProjectCommandValidator(IServiceScopeFactory scopeFactory)
    {
        RuleFor(x => x.ProjectId)
            .MustBeOwnedByCallerAsync(scopeFactory)
            .When(x => !x.OverrideAuthorization);

        RuleFor(x => x.ProjectId)
            .MustExistAsync(scopeFactory)
            .When(x => x.OverrideAuthorization);

        RuleFor(x => x.CommitAuthor).NotEmpty().MaximumLength(255).WithMessage("Commit author is required.");
        RuleFor(x => x.CommitHash).NotEmpty().MaximumLength(255).WithMessage("Commit hash is required.");
        RuleFor(x => x.CommitBranch).NotEmpty().MaximumLength(255).WithMessage("Commit branch is required.");
        RuleFor(x => x.CommitEmail).EmailAddress().NotEmpty().MaximumLength(255).WithMessage("Commit email is required.");
    }
}