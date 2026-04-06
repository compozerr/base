using Api.Data;
using Api.Data.Events;
using Api.Data.Repositories;
using Core.MediatR;
using Database.Extensions;

namespace Api.Endpoints.Projects.Domains.ChangeEnvironment;

public sealed class ChangeDomainEnvironmentCommandHandler(
    IDomainRepository domainRepository) : ICommandHandler<ChangeDomainEnvironmentCommand, ChangeDomainEnvironmentResponse>
{
    public async Task<ChangeDomainEnvironmentResponse> Handle(
        ChangeDomainEnvironmentCommand command, CancellationToken cancellationToken = default)
    {
        var domain = await domainRepository.GetByIdAsync(command.DomainId, cancellationToken)
            ?? throw new ArgumentException($"Domain not found");

        if (domain is not ExternalDomain)
            throw new InvalidOperationException("Cannot change environment routing for internal domains");

        if (domain.ProjectId != command.ProjectId)
            throw new InvalidOperationException("Domain does not belong to this project");

        domain.ProjectEnvironmentId = command.EnvironmentId;
        domain.QueueDomainEvent<DomainChangeEvent>();

        await domainRepository.UpdateAsync(domain, cancellationToken);
        return new();
    }
}
