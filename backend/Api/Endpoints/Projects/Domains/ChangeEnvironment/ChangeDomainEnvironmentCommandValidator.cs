using Api.Data.Extensions;
using FluentValidation;

namespace Api.Endpoints.Projects.Domains.ChangeEnvironment;

public sealed class ChangeDomainEnvironmentCommandValidator : AbstractValidator<ChangeDomainEnvironmentCommand>
{
    public ChangeDomainEnvironmentCommandValidator(IServiceScopeFactory scopeFactory)
    {
        RuleFor(x => x.DomainId).MustBeOwnedByCallerAsync(scopeFactory);
    }
}
