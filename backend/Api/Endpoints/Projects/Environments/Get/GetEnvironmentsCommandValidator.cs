using Api.Data.Extensions;
using FluentValidation;

namespace Api.Endpoints.Projects.Environments.Get;

public sealed class GetEnvironmentsCommandValidator : AbstractValidator<GetEnvironmentsCommand>
{
    public GetEnvironmentsCommandValidator(IServiceScopeFactory scopeFactory)
    {
        RuleFor(x => x.ProjectId)
            .MustBeOwnedByCallerAsync(scopeFactory);
    }
}
