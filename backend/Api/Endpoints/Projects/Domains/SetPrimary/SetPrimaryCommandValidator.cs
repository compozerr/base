using Api.Data.Extensions;
using FluentValidation;

namespace Api.Endpoints.Projects.Domains.SetPrimary;

public sealed class SetPrimaryCommandValidator : AbstractValidator<SetPrimaryCommand>
{
    public SetPrimaryCommandValidator(IServiceScopeFactory scopeFactory)
    {
        RuleFor(x => x.DomainId).MustBeOwnedByCallerAsync(scopeFactory);
    }
}
