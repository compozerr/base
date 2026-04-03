using Api.Abstractions;
using Api.Data.Extensions;
using FluentValidation;

namespace Api.Endpoints.Projects.Environments.Delete;

public sealed class DeleteEnvironmentCommandValidator : AbstractValidator<DeleteEnvironmentCommand>
{
    public DeleteEnvironmentCommandValidator(IServiceScopeFactory scopeFactory)
    {
        RuleFor(x => x.ProjectId)
            .MustBeOwnedByCallerAsync(scopeFactory);
    }
}
