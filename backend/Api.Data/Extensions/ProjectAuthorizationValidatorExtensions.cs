using Api.Data.Repositories;
using Auth.Services;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using Organizations.Services;

namespace Api.Data.Extensions;

public static class ProjectAuthorizationValidatorExtensions
{
	public static IRuleBuilderOptions<T, ProjectId> MustBeOwnedByCallerAsync<T>(this IRuleBuilder<T, ProjectId> ruleBuilder, IServiceScopeFactory serviceScopeFactory)
	{
		return ruleBuilder.MustAsync(async (projectId, cancel) =>
		{
			await using var scope = serviceScopeFactory.CreateAsyncScope();
			var currentUserId = scope.ServiceProvider.GetRequiredService<ICurrentUserAccessor>().CurrentUserId;
			var projectRepository = scope.ServiceProvider.GetRequiredService<IProjectRepository>();

			if (projectId == null || currentUserId == null)
				return false;

			var project = await projectRepository.GetByIdAsync(
				projectId,
				cancel,
				getDeleted: true);

			if (project is null)
				return false;

			// If project has an OrganizationId, check org membership
			if (project.OrganizationId is not null)
			{
				var orgAuthService = scope.ServiceProvider.GetRequiredService<IOrganizationAuthorizationService>();
				return await orgAuthService.CanAccessProjectAsync(project.OrganizationId, currentUserId, cancel);
			}

			// Fallback to legacy user-based ownership check
			return project.UserId == currentUserId;
		})
		.WithMessage("You do not have permission to access this project.")
		.WithName("ProjectId");
	}

	public static IRuleBuilderOptions<T, ProjectId> MustBeAdminOfProjectOrgAsync<T>(this IRuleBuilder<T, ProjectId> ruleBuilder, IServiceScopeFactory serviceScopeFactory)
	{
		return ruleBuilder.MustAsync(async (projectId, cancel) =>
		{
			await using var scope = serviceScopeFactory.CreateAsyncScope();
			var currentUserId = scope.ServiceProvider.GetRequiredService<ICurrentUserAccessor>().CurrentUserId;
			var projectRepository = scope.ServiceProvider.GetRequiredService<IProjectRepository>();

			if (projectId == null || currentUserId == null)
				return false;

			var project = await projectRepository.GetByIdAsync(projectId, cancel, getDeleted: true);
			if (project is null)
				return false;

			if (project.OrganizationId is not null)
			{
				var orgAuthService = scope.ServiceProvider.GetRequiredService<IOrganizationAuthorizationService>();
				return await orgAuthService.IsAdminAsync(project.OrganizationId, currentUserId, cancel);
			}

			return project.UserId == currentUserId;
		})
		.WithMessage("Only organization admins can perform this action.")
		.WithName("ProjectId");
	}

	public static IRuleBuilderOptions<T, ProjectId> MustExistAsync<T>(this IRuleBuilder<T, ProjectId> ruleBuilder, IServiceScopeFactory serviceScopeFactory)
	{
		return ruleBuilder.MustAsync(async (projectId, cancel) =>
		{
			await using var scope = serviceScopeFactory.CreateAsyncScope();
			var projectRepository = scope.ServiceProvider.GetRequiredService<IProjectRepository>();

			if (projectId == null)
				return false;

			var project = await projectRepository.GetByIdAsync(
				projectId,
				cancel);

			return project is not null;
		})
		.WithMessage("The specified project does not exist.")
		.WithName("ProjectId");
	}
}