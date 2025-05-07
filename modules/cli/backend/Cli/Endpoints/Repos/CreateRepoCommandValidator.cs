using Api.Data.Repositories;
using Auth.Services;
using FluentValidation;
using Github.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Cli.Endpoints.Repos;

public sealed class CreateRepoCommandValidator : AbstractValidator<CreateRepoCommand>
{
    public CreateRepoCommandValidator(IServiceScopeFactory scopeFactory)
    {
        var scope = scopeFactory.CreateScope();
        var githubService = scope.ServiceProvider.GetRequiredService<IGithubService>();
        var currentUserAccessor = scope.ServiceProvider.GetRequiredService<ICurrentUserAccessor>();
        var locationRepository = scope.ServiceProvider.GetRequiredService<ILocationRepository>();

        RuleFor(x => x.Name)
            .Matches(@"^[a-z0-9]+(?:(?:(?:[._]|__|[-]*)[a-z0-9]+)+)?$")
            .WithMessage("Repository name must adhere to the pattern [a-z0-9]+(?:(?:(?:[._]|__|[-]*)[a-z0-9]+)+)?");

        RuleFor(x => x.Name).MustAsync(async (command, name, cancellationToken) =>
        {
            var reposForCurrentUser = await githubService.GetRepositoriesByUserDefaultIdAsync(
                currentUserAccessor.CurrentUserId!,
                command.Type);

            return !reposForCurrentUser.Any(r => r.Name == name);
        });

        RuleFor(x => x.LocationIsoCode).MustAsync(async (command, locationIsoCode, cancellationToken) =>
        {
            if (command.Type != Github.Endpoints.SetDefaultInstallationId.DefaultInstallationIdSelectionType.Projects) return true;

            var availableLocationIsoCodes = await locationRepository.GetUniquePublicLocationIsoCodes();

            return availableLocationIsoCodes.Contains(locationIsoCode);
        });
    }
}