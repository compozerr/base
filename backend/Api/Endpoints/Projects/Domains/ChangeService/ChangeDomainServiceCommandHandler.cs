using Api.Data;
using Api.Data.Events;
using Api.Data.Repositories;
using Core.MediatR;
using Database.Extensions;
using Microsoft.EntityFrameworkCore;

namespace Api.Endpoints.Projects.Domains.ChangeService;

public sealed class ChangeDomainServiceCommandHandler(
    IDomainRepository domainRepository,
    ApiDbContext context) : ICommandHandler<ChangeDomainServiceCommand, ChangeDomainServiceResponse>
{
    public async Task<ChangeDomainServiceResponse> Handle(
        ChangeDomainServiceCommand command, CancellationToken cancellationToken = default)
    {
        var domain = await domainRepository.GetByIdAsync(command.DomainId, cancellationToken)
            ?? throw new ArgumentException("Domain not found");

        if (domain is not ExternalDomain)
            throw new InvalidOperationException("Cannot change service routing for internal domains");

        if (domain.ProjectId != command.ProjectId)
            throw new InvalidOperationException("Domain does not belong to this project");

        var service = await context.ProjectServices
            .FirstOrDefaultAsync(s => s.ProjectId == command.ProjectId && s.Name == command.ServiceName, cancellationToken)
            ?? throw new InvalidOperationException($"Service '{command.ServiceName}' not found for project");

        domain.ServiceName = command.ServiceName;
        domain.Port = service.Port;
        domain.Protocol = command.Protocol;
        domain.QueueDomainEvent<DomainChangeEvent>();

        await domainRepository.UpdateAsync(domain, cancellationToken);
        return new();
    }
}
