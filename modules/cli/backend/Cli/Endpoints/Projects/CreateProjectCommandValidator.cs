using Api.Abstractions;
using Api.Data.Repositories;
using Auth.Services;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using Stripe.Extensions;

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
            // Enforce uniqueness per user
            var projectsForCurrentUser = await projectRepository.GetProjectsForUserAsync(
                currentUserAccessor.CurrentUserId!);

            return !projectsForCurrentUser.Any(r => r.RepoUri.ToString() == repoUrl);
        }).WithMessage("You already have a project with this repository URL.")
          .NotEmpty().WithMessage("Repository URL cannot be empty.")
          .NotNull().WithMessage("Repository URL cannot be null.");

        RuleFor(x => x.Tier).Must(tier =>
        {
            return ServerTiers.All.Select(x => x.Id.Value).Contains(tier);
        }).WithMessage("Invalid tier specified.")
          .NotEmpty().WithMessage("Tier cannot be empty.")
          .NotNull().WithMessage("Tier cannot be null.");

        RuleFor(x => x).UserMustHavePaymentMethod(scopeFactory);
    }
}