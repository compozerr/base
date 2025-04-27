using Api.Data.Extensions;
using FluentValidation;

namespace Api.Endpoints.Projects.Project.Stop;

public sealed class StopProjectCommandValidator : AbstractValidator<StopProjectCommand>
{
    public StopProjectCommandValidator(IServiceScopeFactory scopeFactory)
    {
        RuleFor(x => x.ProjectId).MustBeOwnedByCallerAsync(scopeFactory);
    }
}
