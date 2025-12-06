using Api.Data.Repositories;
using Auth.Services;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace Api.Endpoints.Projects.Domains.Delete;

public sealed class DeleteDomainCommandValidator : AbstractValidator<DeleteDomainCommand>
{
    public DeleteDomainCommandValidator(IServiceScopeFactory scopeFactory)
    {
        RuleFor(x => x.DomainId).MustAsync(async (command, domainId, cancellationToken) =>
        {
            await using var scope = scopeFactory.CreateAsyncScope();
            var domainRepository = scope.ServiceProvider.GetRequiredService<IDomainRepository>();
            var currentUserAccessor = scope.ServiceProvider.GetRequiredService<ICurrentUserAccessor>();

            var domain = await domainRepository.GetByIdAsync(
                domainId,
                x => x.Include(d => d.Project),
                cancellationToken);

            return domain?.Project?.UserId == currentUserAccessor.CurrentUserId;
        }).WithErrorCode("403");
    }
}
