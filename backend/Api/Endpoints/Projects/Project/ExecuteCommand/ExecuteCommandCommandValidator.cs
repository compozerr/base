using Api.Data.Extensions;
using FluentValidation;

namespace Api.Endpoints.Projects.Project.ExecuteCommand;

public sealed class ExecuteCommandCommandValidator : AbstractValidator<ExecuteCommandCommand>
{
    public ExecuteCommandCommandValidator(IServiceScopeFactory scopeFactory)
    {
        RuleFor(x => x.ProjectId).MustBeOwnedByCallerAsync(scopeFactory);
        RuleFor(x => x.Command).NotEmpty();
    }
}
