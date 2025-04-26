using Api.Data.Repositories;
using Auth.Services;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;

namespace Api.Endpoints.Projects.Usage.Get;

public sealed class GetUsageCommandValidator : AbstractValidator<GetUsageCommand>
{
    public GetUsageCommandValidator(IServiceScopeFactory scopeFactory)
    {
        var scope = scopeFactory.CreateScope();
        var projectRepository = scope.ServiceProvider.GetRequiredService<IProjectRepository>();
        var currentUserAccessor = scope.ServiceProvider.GetRequiredService<ICurrentUserAccessor>();

        RuleFor(x => x.ProjectId).MustAsync(async (command, projectId, cancellationToken) =>
        {
            var project = await projectRepository.GetByIdAsync(projectId, cancellationToken);

            return currentUserAccessor.CurrentUserId == project?.UserId;
        }).WithMessage("You do not have access to this project.");
    }
}
