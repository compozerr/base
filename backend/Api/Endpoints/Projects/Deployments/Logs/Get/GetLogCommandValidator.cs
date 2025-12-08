using Api.Data.Repositories;
using Auth.Services;
using FluentValidation;

namespace Api.Endpoints.Projects.Deployments.Logs.Get;

public sealed class GetLogCommandValidator : AbstractValidator<GetLogCommand>
{
    public GetLogCommandValidator(
        IServiceScopeFactory scopeFactory)
    {
        RuleFor(x => x.ProjectId).MustAsync(async (projectId, cancellationToken) =>
        {
            await using var scope = scopeFactory.CreateAsyncScope();
            var projectRepository = scope.ServiceProvider.GetRequiredService<IProjectRepository>();
            var currentUserAccessor = scope.ServiceProvider.GetRequiredService<ICurrentUserAccessor>();
            
            var project = await projectRepository.GetByIdAsync(projectId, cancellationToken);

            return currentUserAccessor.CurrentUserId == project?.UserId;
        }).WithMessage("User does not have access to this project");

    }
}
