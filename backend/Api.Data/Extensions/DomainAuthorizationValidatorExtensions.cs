using Api.Data.Repositories;
using Auth.Services;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Organizations.Services;

namespace Api.Data.Extensions;

public static class DomainAuthorizationValidatorExtensions
{
    public static IRuleBuilderOptions<T, DomainId> MustBeOwnedByCallerAsync<T>(
        this IRuleBuilder<T, DomainId> ruleBuilder,
        IServiceScopeFactory serviceScopeFactory)
    {
        return ruleBuilder.MustAsync(async (domainId, cancel) =>
        {
            await using var scope = serviceScopeFactory.CreateAsyncScope();
            var currentUserId = scope.ServiceProvider.GetRequiredService<ICurrentUserAccessor>().CurrentUserId;
            var domainRepository = scope.ServiceProvider.GetRequiredService<IDomainRepository>();

            if (domainId is null || currentUserId is null)
                return false;

            var domain = await domainRepository.GetByIdAsync(
                domainId,
                x => x.Include(d => d.Project),
                cancel);

            if (domain?.Project is null)
                return false;

            if (domain.Project.OrganizationId is not null)
            {
                var orgAuthService = scope.ServiceProvider.GetRequiredService<IOrganizationAuthorizationService>();
                return await orgAuthService.CanAccessProjectAsync(domain.Project.OrganizationId, currentUserId, cancel);
            }

            return domain.Project.UserId == currentUserId;
        })
        .WithErrorCode("403")
        .WithMessage("You do not have permission to access this domain.")
        .WithName("DomainId");
    }
}
