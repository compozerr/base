using Api.Data;
using Api.Data.Repositories;
using Core.MediatR;
using Database.Extensions;
using Microsoft.EntityFrameworkCore;

namespace Api.Endpoints.Projects.Domains.Add;

public sealed class AddDomainCommandHandler(
    IDomainRepository domainRepository,
    ApiDbContext context) : ICommandHandler<AddDomainCommand, AddDomainResponse>
{
    public async Task<AddDomainResponse> Handle(
        AddDomainCommand command,
        CancellationToken cancellationToken = default)
    {
        var service = await context.ProjectServices
            .FirstOrDefaultAsync(s => s.ProjectId == command.ProjectId && s.Name == command.ServiceName, cancellationToken);

        if (service is null)
            throw new InvalidOperationException($"Service '{command.ServiceName}' not found for project");

        var externalDomainEntity = new ExternalDomain
        {
            ProjectId = command.ProjectId,
            ServiceName = command.ServiceName,
            Port = service.Port,
            Value = command.Domain,
            IsVerified = false,
        };

        externalDomainEntity.QueueDomainEvent<ExternalDomainAddedEvent>();

        var domain = await domainRepository.AddAsync(externalDomainEntity, cancellationToken);

        return new(domain.Id.Value.ToString());
    }
}
