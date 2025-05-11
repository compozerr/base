using FluentValidation;
using Microsoft.Extensions.DependencyInjection;

namespace Cli.Endpoints.Modules.Add;

public sealed class AddModuleCommandValidator : AbstractValidator<AddModuleCommand>
{
    public AddModuleCommandValidator(IServiceScopeFactory scopeFactory)
    {
        var scope = scopeFactory.CreateScope();
        // Add required services using scope.ServiceProvider.GetRequiredService<T>()

        // Add validation rules using RuleFor()
    }
}
