using Api.Abstractions;
using Api.Data.Extensions;
using FluentValidation;
using MediatR;

namespace Api.Endpoints.Projects.Project.ChangeTier;

public sealed class ChangeTierCommandValidator : AbstractValidator<ChangeTierCommand>
{
	public ChangeTierCommandValidator(IServiceScopeFactory scopeFactory)
	{
		using var scope = scopeFactory.CreateScope();

		scope.ServiceProvider.GetRequiredService<IMediator>();

		RuleFor(x => x.Tier).Must(tier =>
		{
			return ServerTiers.All.Select(x => x.Id.Value).Contains(tier);
		}).WithMessage("Invalid tier specified.")
		  .NotEmpty().WithMessage("Tier cannot be empty.")
		  .NotNull().WithMessage("Tier cannot be null.");

		RuleFor(x => x.ProjectId).MustBeOwnedByCallerAsync(scopeFactory);
	}
}
