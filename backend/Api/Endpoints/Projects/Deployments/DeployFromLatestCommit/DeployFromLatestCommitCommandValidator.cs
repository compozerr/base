using Api.Data.Extensions;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;

namespace Api.Endpoints.Projects.Deployments.DeployFromLatestCommit;

public sealed class DeployFromLatestCommitCommandValidator : AbstractValidator<DeployFromLatestCommitCommand>
{
	public DeployFromLatestCommitCommandValidator(IServiceScopeFactory scopeFactory)
	{
		var scope = scopeFactory.CreateScope();
		
		RuleFor(x => x.ProjectId)
			.MustBeOwnedByCallerAsync(scopeFactory);
	}
}
