using Api.Data;
using Api.Data.Repositories;
using Core.MediatR;
using Database.Extensions;

namespace Api.Endpoints.Projects.Domains.Add;

public sealed class AddDomainCommandHandler(
    IDomainRepository domainRepository) : ICommandHandler<AddDomainCommand, AddDomainResponse>
{
    public async Task<AddDomainResponse> Handle(
        AddDomainCommand command,
        CancellationToken cancellationToken = default)
    {
        var externalDomainEntity = new ExternalDomain
        {
            ProjectId = command.ProjectId,
            ServiceName = command.ServiceName,
            Port = GetServicePort(command.ServiceName),
            Value = command.Domain,
            IsVerified = false,
        };

        externalDomainEntity.QueueDomainEvent<ExternalDomainAddedEvent>();

        var domain = await domainRepository.AddAsync(externalDomainEntity, cancellationToken);

        return new(domain.Id.Value.ToString());
    }

    //TODO: This should be its own entity
    private static string GetServicePort(string serviceName)
    {
        return serviceName switch
        {
            "Frontend" => "1234",
            "Backend" => "1235",
            _ => throw new ArgumentException("Service name must be either 'Frontend' or 'Backend'")
        };
    }
}
