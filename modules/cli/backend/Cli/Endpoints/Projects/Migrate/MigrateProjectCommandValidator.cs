using Api.Abstractions;
using Api.Data.Repositories;
using Auth.Services;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using Stripe.Extensions;

namespace Cli.Endpoints.Projects.Migrate;

public sealed class MigrateProjectCommandValidator : AbstractValidator<MigrateProjectCommand>
{
    public MigrateProjectCommandValidator(IServiceScopeFactory scopeFactory)
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Project name cannot be empty.")
            .NotNull().WithMessage("Project name cannot be null.")
            .MaximumLength(255).WithMessage("Project name cannot exceed 255 characters.");

        RuleFor(x => x.RepoUrl)
            .NotEmpty().WithMessage("Repository URL cannot be empty.")
            .NotNull().WithMessage("Repository URL cannot be null.")
            .MustAsync(async (command, repoUrl, cancellationToken) =>
            {
                await using var scope = scopeFactory.CreateAsyncScope();
                var projectRepository = scope.ServiceProvider.GetRequiredService<IProjectRepository>();
                var currentUserAccessor = scope.ServiceProvider.GetRequiredService<ICurrentUserAccessor>();

                var projectsForCurrentUser = await projectRepository.GetProjectsForUserAsync(
                    currentUserAccessor.CurrentUserId!);

                return !projectsForCurrentUser.Any(r => r.RepoUri.ToString() == repoUrl);
            }).WithMessage("You already have a project with this repository URL.");

        RuleFor(x => x.Tier)
            .NotEmpty().WithMessage("Tier cannot be empty.")
            .NotNull().WithMessage("Tier cannot be null.")
            .Must(tier => ServerTiers.All.Select(x => x.Id.Value).Contains(tier))
            .WithMessage("Invalid tier specified.");

        RuleFor(x => x.LocationIso)
            .NotEmpty().WithMessage("Location cannot be empty.")
            .NotNull().WithMessage("Location cannot be null.");

        RuleFor(x => x).UserMustHavePaymentMethod(scopeFactory);
    }
}
