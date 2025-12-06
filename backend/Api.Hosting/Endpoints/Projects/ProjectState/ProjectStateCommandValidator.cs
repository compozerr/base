using FluentValidation;
using Microsoft.Extensions.DependencyInjection;

namespace Api.Hosting.Endpoints.Projects.ProjectState;

public sealed class ProjectStateCommandValidator : AbstractValidator<ProjectStateCommand>
{
	public ProjectStateCommandValidator()
	{
		// Add validation rules using RuleFor()
	}
}
