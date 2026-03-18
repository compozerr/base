using Api.Data.Repositories;
using Auth.Services;
using FluentValidation;
using Organizations.Services;

namespace Api.Endpoints.Projects.ProjectEnvironment;

public sealed class UpsertProjectEnvironmentVariablesCommandValidator : AbstractValidator<UpsertProjectEnvironmentVariablesCommand>
{
    public UpsertProjectEnvironmentVariablesCommandValidator(IServiceScopeFactory scopeFactory)
    {
        RuleFor(x => x.ProjectId).MustAsync(async (command, projectId, cancellationToken) =>
        {
            await using var scope = scopeFactory.CreateAsyncScope();
            var projectRepository = scope.ServiceProvider.GetRequiredService<IProjectRepository>();
            var currentUserAccessor = scope.ServiceProvider.GetRequiredService<ICurrentUserAccessor>();
            var currentUserId = currentUserAccessor.CurrentUserId;
            if (currentUserId is null) return false;

            var project = await projectRepository.GetByIdAsync(projectId, cancellationToken);
            if (project is null) return false;

            if (project.OrganizationId is not null)
            {
                var orgAuthService = scope.ServiceProvider.GetRequiredService<IOrganizationAuthorizationService>();
                return await orgAuthService.CanAccessProjectAsync(project.OrganizationId, currentUserId, cancellationToken);
            }

            return project.UserId == currentUserId;
        }).WithErrorCode("403");

        RuleFor(x => x.Branch).MustAsync(async (command, branch, cancellationToken) =>
        {
            await using var scope = scopeFactory.CreateAsyncScope();
            var projectEnvironmentRepository = scope.ServiceProvider.GetRequiredService<IProjectEnvironmentRepository>();

            var environment = await projectEnvironmentRepository.GetProjectEnvironmentByBranchAsync(
                command.ProjectId,
                branch);

            return environment is not null;
        }).WithMessage("Branch does not exist");

        RuleForEach(x => x.Variables).ChildRules(v =>
        {
            v.RuleFor(x => x.Key)
                .NotEmpty()
                .WithMessage("Key cannot be empty")
                .MaximumLength(255)
                .WithMessage("Key cannot be longer than 100 characters");

            v.RuleFor(x => x.Value)
                .NotEmpty()
                .WithMessage("Value cannot be empty")
                .MaximumLength(20 * 1024)
                .WithMessage("Value cannot be longer than 20KB");
        });
    }
}
