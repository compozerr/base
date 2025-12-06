using FluentValidation;
using Microsoft.Extensions.DependencyInjection;

namespace Cli.Endpoints.Modules.Add;

public sealed class AddModuleCommandValidator : AbstractValidator<AddModuleCommand>
{
    public AddModuleCommandValidator()
    {
    }
}
