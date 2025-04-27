using Api.Data.Extensions;
using FluentValidation;

namespace Api.Endpoints.Projects.Project.Start;

public sealed class StartProjectCommandValidator : AbstractValidator<StartProjectCommand>
{
    public StartProjectCommandValidator(IServiceScopeFactory scopeFactory)
    {
        RuleFor(x => x.ProjectId).MustBeOwnedByCallerAsync(scopeFactory);
    }
}
