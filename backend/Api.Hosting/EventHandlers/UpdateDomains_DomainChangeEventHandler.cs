using Api.Data.Events;
using Api.Data.Repositories;
using Api.Hosting.Services;
using Core.Abstractions;
using Core.Extensions;

namespace Api.Hosting.EventHandlers;

public sealed class UpdateDomains_DomainChangeEventHandler(
    IProjectRepository projectRepository,
    IHostingApiFactory hostingApiFactory) : IDomainEventHandler<DomainChangeEvent>
{
    public async Task Handle(DomainChangeEvent notification, CancellationToken cancellationToken)
    {
        var domainProject = await projectRepository.GetByIdAsync(
            notification.Entity.ProjectId,
            cancellationToken) ?? throw new ArgumentException("Project not found");

        var hostingApi = await hostingApiFactory.GetHostingApiAsync(domainProject.ServerId ?? throw new ArgumentException("Server id not found"));

        hostingApi.UpdateDomainsForProjectAsync(notification.Entity.ProjectId)
                  .LogAndSilence();
                  
    }
}
