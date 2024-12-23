using Auth.Services;
using FluentValidation;
using Github.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Cli.Endpoints.Repos;

public sealed class CreateRepoCommandValidator : AbstractValidator<CreateRepoCommand>
{
    public CreateRepoCommandValidator(IServiceScopeFactory scopeFactory)
    {
        using var scope = scopeFactory.CreateScope();
        var githubService = scope.ServiceProvider.GetRequiredService<IGithubService>();
        var currentUserAccessor = scope.ServiceProvider.GetRequiredService<ICurrentUserAccessor>();

        RuleFor(x => x.Name).MustAsync(async (command, name, cancellationToken) =>
        {
            var reposForCurrentUser = await githubService.GetRepositoriesByUserDefaultIdAsync(
                currentUserAccessor.CurrentUserId!,
                command.Type);

            return !reposForCurrentUser.Any(r => r.Name == name);
        });
    }
}