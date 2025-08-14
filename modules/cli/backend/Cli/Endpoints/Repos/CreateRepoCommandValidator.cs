using Api.Abstractions;
using Api.Data.Extensions;
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
        var projectRepository = scope.ServiceProvider.GetRequiredService<IProjectRepository>();

        RuleFor(x => x.Name)
            .Matches(@"^[a-z0-9]+(?:(?:(?:[._]|__|[-]*)[a-z0-9]+)+)?$")
            .WithMessage("Repository name must adhere to the pattern [a-z0-9]+(?:(?:(?:[._]|__|[-]*)[a-z0-9]+)+)? (e.g. my-module).");

        RuleFor(x => x.Name).MustAsync(async (command, name, cancellationToken) =>
        {
            var reposForCurrentUser = await githubService.GetRepositoriesByUserDefaultIdAsync(
                currentUserAccessor.CurrentUserId!,
                command.Type);

            return !reposForCurrentUser.Any(r => r.Name == name);
        }).WithMessage("Repository name must be unique.");

        When(x => x.ProjectId != null, () =>
        {
            RuleFor(x => x.ProjectId!).MustBeOwnedByCallerAsync(scopeFactory);
        });

        When(x => x.Type == Github.Endpoints.SetDefaultInstallationId.DefaultInstallationIdSelectionType.Projects, () =>
        {
            RuleFor(x => x.Tier).Must(tier =>
            {
                return ServerTiers.All.Select(x => x.Id.Value).Contains(tier);
            }).WithMessage("Invalid tier specified.")
            .NotEmpty().WithMessage("Tier cannot be empty.")
            .NotNull().WithMessage("Tier cannot be null.");
        });

        RuleFor(x => x.LocationIsoCode).MustAsync(async (command, locationIsoCode, cancellationToken) =>
        {
            if (command.Type != Github.Endpoints.SetDefaultInstallationId.DefaultInstallationIdSelectionType.Projects) return true;

            var availableLocationIsoCodes = await locationRepository.GetUniquePublicLocationIsoCodes();

            return availableLocationIsoCodes.Contains(locationIsoCode);
        });
    }
}