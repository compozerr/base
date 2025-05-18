using Api.Data.Repositories;
using Auth.Services;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;

namespace Api.Data.Extensions;

public static class ProjectAuthorizationValidatorExtensions
{
	public static IRuleBuilderOptions<T, ProjectId> MustBeOwnedByCallerAsync<T>(this IRuleBuilder<T, ProjectId> ruleBuilder, IServiceScopeFactory serviceScopeFactory)
	{
		return ruleBuilder.MustAsync(async (projectId, cancel) =>
		{
			using var scope = serviceScopeFactory.CreateScope();
			var currentUserId = scope.ServiceProvider.GetRequiredService<ICurrentUserAccessor>().CurrentUserId;
			var projectRepository = scope.ServiceProvider.GetRequiredService<IProjectRepository>();

			if (projectId == null)
				return false;

			var project = await projectRepository.GetByIdAsync(
				projectId,
				cancel,
				getDeleted: true);

			return project?.UserId == currentUserId;
		})
		.WithMessage("You do not have permission to access this project.")
		.WithName("ProjectId");
	}
}