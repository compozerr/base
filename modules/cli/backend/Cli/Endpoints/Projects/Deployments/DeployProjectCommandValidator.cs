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
            var projectsForCurrentUser = await projectRepository.GetProjectsForUserAsync(
                currentUserAccessor.CurrentUserId!);

            return projectsForCurrentUser.Any(r => r.Id == projectId);
        });

        RuleFor(x => x.CommitAuthor).NotEmpty().MaximumLength(255);
        RuleFor(x => x.CommitHash).NotEmpty().MaximumLength(255);
        RuleFor(x => x.CommitBranch).NotEmpty().MaximumLength(255);
        RuleFor(x => x.CommitEmail).EmailAddress().NotEmpty().MaximumLength(255);
    }
}