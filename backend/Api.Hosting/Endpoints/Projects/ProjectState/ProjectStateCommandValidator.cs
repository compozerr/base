using FluentValidation;
using Microsoft.Extensions.DependencyInjection;

namespace Api.Hosting.Endpoints.Projects.ProjectState;

public sealed class ProjectStateCommandValidator : AbstractValidator<ProjectStateCommand>
{
	public ProjectStateCommandValidator(IServiceScopeFactory scopeFactory)
	{
		var scope = scopeFactory.CreateScope();
		// Add required services using scope.ServiceProvider.GetRequiredService<T>()

		// Add validation rules using RuleFor()
	}
}
