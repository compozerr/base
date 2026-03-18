using Api.Data.Repositories;
using Auth.Services;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Organizations.Services;

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
            var currentUserId = currentUserAccessor.CurrentUserId;
            if (currentUserId is null) return false;

            var domain = await domainRepository.GetByIdAsync(
                domainId,
                x => x.Include(d => d.Project),
                cancellationToken);

            if (domain?.Project is null) return false;

            if (domain.Project.OrganizationId is not null)
            {
                var orgAuthService = scope.ServiceProvider.GetRequiredService<IOrganizationAuthorizationService>();
                return await orgAuthService.CanAccessProjectAsync(domain.Project.OrganizationId, currentUserId, cancellationToken);
            }

            return domain.Project.UserId == currentUserId;
        }).WithErrorCode("403");
    }
}
