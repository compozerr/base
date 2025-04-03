using Api.Data.Repositories;
using Auth.Services;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;

namespace Cli.Endpoints.Projects.Deployments;

public sealed class DeployProjectCommandValidator : AbstractValidator<DeployProjectCommand>
{
    public DeployProjectCommandValidator(IServiceScopeFactory scopeFactory)
    {
        var scope = scopeFactory.CreateScope();
        var projectRepository = scope.ServiceProvider.GetRequiredService<IProjectRepository>();
        var currentUserAccessor = scope.ServiceProvider.GetRequiredService<ICurrentUserAccessor>();

        RuleFor(x => x.ProjectId).MustAsync(async (command, projectId, cancellationToken) =>
        {
            var project = await projectRepository.GetByIdAsync(projectId, cancellationToken);

            return currentUserAccessor.CurrentUserId == project?.UserId;
        }).WithMessage("You do not have access to this project.");

        RuleFor(x => x.CommitAuthor).NotEmpty().MaximumLength(255).WithMessage("Commit author is required.");
        RuleFor(x => x.CommitHash).NotEmpty().MaximumLength(255).WithMessage("Commit hash is required.");
        RuleFor(x => x.CommitBranch).NotEmpty().MaximumLength(255).WithMessage("Commit branch is required.");
        RuleFor(x => x.CommitEmail).EmailAddress().NotEmpty().MaximumLength(255).WithMessage("Commit email is required.");
    }
}