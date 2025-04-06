using Api.Data.Repositories;
using Auth.Services;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace Api.Endpoints.Projects.Domains.SetPrimary;

public sealed class SetPrimaryCommandValidator : AbstractValidator<SetPrimaryCommand>
{
    public SetPrimaryCommandValidator(IServiceScopeFactory scopeFactory)
    {
        var scope = scopeFactory.CreateScope();

        var domainRepository = scope.ServiceProvider.GetRequiredService<IDomainRepository>();
        var currentUserAccessor = scope.ServiceProvider.GetRequiredService<ICurrentUserAccessor>();

        RuleFor(x => x.DomainId)
            .MustAsync(async (domainId, cancellationToken) =>
            {
                var domain = await domainRepository.GetByIdAsync(
                    domainId,
                    x => x.Include(y => y.Project),
                    cancellationToken);

                return domain is { Project.UserId: { } userId } && userId == currentUserAccessor.CurrentUserId;
            });
    }
}
