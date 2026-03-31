using Api.Data.Repositories;
using Auth.Services;
using FluentValidation;
using Organizations.Services;

namespace Api.Endpoints.Projects.Environments.Create;

public sealed class CreateEnvironmentCommandValidator : AbstractValidator<CreateEnvironmentCommand>
{
    public CreateEnvironmentCommandValidator(IServiceScopeFactory scopeFactory)
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .WithMessage("Name cannot be empty")
            .MaximumLength(100)
            .WithMessage("Name cannot be longer than 100 characters");

        RuleFor(x => x.Branch)
            .NotEmpty()
            .WithMessage("Branch cannot be empty")
            .MaximumLength(255)
            .WithMessage("Branch cannot be longer than 255 characters");

        RuleFor(x => x.Type)
            .IsInEnum()
            .WithMessage("Invalid environment type");

        RuleFor(x => x.ProjectId)
            .MustAsync(async (command, projectId, cancellationToken) =>
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
            })
            .WithErrorCode("403");
    }
}
