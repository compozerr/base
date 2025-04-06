using Api.Data;
using Api.Data.Repositories;
using Core.Abstractions;

namespace Api.Endpoints.Projects.Domains.Add;

public sealed class HandlePrimaryDomain_ExternalDomainAddedEventHandler(
    IDomainRepository domainRepository) : IDomainEventHandler<ExternalDomainAddedEvent>
{
    public async Task Handle(ExternalDomainAddedEvent notification, CancellationToken cancellationToken)
    {
        var currentDomains = await domainRepository.GetAllAsync(
           x => x.Where(x => x.ProjectId == notification.Entity.ProjectId),
           cancellationToken);

        if (!currentDomains.Where(x => x.Type == DomainType.External).Any())
        {
            notification.Entity.IsPrimary = true;
        }

        // Set others as not primary
        foreach (var domain in currentDomains)
        {
            domain.IsPrimary = false;
            await domainRepository.UpdateAsync(
                domain,
                cancellationToken);
        }
    }
}
