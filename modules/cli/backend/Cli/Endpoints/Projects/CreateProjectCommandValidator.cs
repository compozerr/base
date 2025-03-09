using Api.Data.Repositories;
using Auth.Services;
using FluentValidation;
using Github.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Cli.Endpoints.Projects;

public sealed class CreateProjectCommandValidator : AbstractValidator<CreateProjectCommand>
{
    public CreateProjectCommandValidator(IServiceScopeFactory scopeFactory)
    {
        var scope = scopeFactory.CreateScope();
        var projectRepository = scope.ServiceProvider.GetRequiredService<IProjectRepository>();
        var currentUserAccessor = scope.ServiceProvider.GetRequiredService<ICurrentUserAccessor>();

        RuleFor(x => x.RepoUrl).MustAsync(async (command, repoUrl, cancellationToken) =>
        {
            var projectsForCurrentUser = await projectRepository.GetProjectsForUserAsync(
                currentUserAccessor.CurrentUserId!);

            return !projectsForCurrentUser.Any(r => r.RepoUri.ToString() == repoUrl);
        });
    }
}