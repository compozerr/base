using Api.Data;
using Api.Data.Repositories;
using Core.Abstractions;

namespace Api.Endpoints.Projects.Domains.Add;

public sealed class HandlePrimaryDomain_ExternalDomainAddedEventHandler(
    IDomainRepository domainRepository) : EntityDomainEventHandlerBase<ExternalDomainAddedEvent>
{
    protected override async Task HandleBeforeSaveAsync(ExternalDomainAddedEvent domainEvent, CancellationToken cancellationToken)
    {
        var currentDomains = await domainRepository.GetAllAsync(
           x => x.Where(x => x.ProjectId == domainEvent.Entity.ProjectId),
           cancellationToken);

        if (!currentDomains.Where(x => x.Type == DomainType.External).Any())
        {
            domainEvent.Entity.IsPrimary = true;
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
