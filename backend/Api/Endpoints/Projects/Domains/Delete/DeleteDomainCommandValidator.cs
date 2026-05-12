using Api.Data.Extensions;
using FluentValidation;

namespace Api.Endpoints.Projects.Domains.Delete;

public sealed class DeleteDomainCommandValidator : AbstractValidator<DeleteDomainCommand>
{
    public DeleteDomainCommandValidator(IServiceScopeFactory scopeFactory)
    {
        RuleFor(x => x.DomainId).MustBeOwnedByCallerAsync(scopeFactory);
    }
}
