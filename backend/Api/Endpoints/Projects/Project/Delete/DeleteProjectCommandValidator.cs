using Api.Data.Extensions;
using FluentValidation;

namespace Api.Endpoints.Projects.Project.Delete;

public sealed class DeleteProjectCommandValidator : AbstractValidator<DeleteProjectCommand>
{
    public DeleteProjectCommandValidator(IServiceScopeFactory scopeFactory)
    {
        RuleFor(x => x.ProjectId)
            .MustBeOwnedByCallerAsync(scopeFactory)
            .When(x => !x.SkipOwnerCheck);
    }
}
