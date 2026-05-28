using Api.Data.Events;
using Api.Hosting.Services.EnvironmentTargetResolver;
using Core.Abstractions;
using Core.Extensions;

namespace Api.Hosting.EventHandlers;

public sealed class UpdateDomains_DomainChangeEventHandler(
    IEnvironmentTargetResolver environmentTargetResolver) : EntityDomainEventHandlerBase<DomainChangeEvent>
{
    protected override async Task HandleAfterSaveAsync(DomainChangeEvent domainEvent, CancellationToken cancellationToken)
    {
        // Fan out to every distinct server hosting an env of this project so
        // every Traefik config is in sync. Spec B: envs of the same project
        // may live on different servers. Per-server UpdateDomainsForProjectAsync
        // is idempotent so a duplicate push is harmless.
        var hostingApis = await environmentTargetResolver
            .GetDistinctHostingApisForProjectAsync(domainEvent.Entity.ProjectId, cancellationToken);

        foreach (var hostingApi in hostingApis)
        {
            hostingApi.UpdateDomainsForProjectAsync(domainEvent.Entity.ProjectId)
                      .LogAndSilence();
        }
    }
}
