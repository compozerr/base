using FluentValidation;
using Microsoft.Extensions.DependencyInjection;

namespace Api.Hosting.Endpoints.VMPooling.InitiatePoolSync;

public sealed class InitiatePoolSyncCommandValidator : AbstractValidator<InitiatePoolSyncCommand>
{
	public InitiatePoolSyncCommandValidator(IServiceScopeFactory scopeFactory)
	{
		var scope = scopeFactory.CreateScope();
	}
}
