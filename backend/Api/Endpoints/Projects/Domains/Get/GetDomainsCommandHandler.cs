using Api.Data;
using Api.Data.Repositories;
using Core.MediatR;

namespace Api.Endpoints.Projects.Domains.Get;

public sealed class GetDomainsCommandHandler(
    IProjectRepository projectRepository) : ICommandHandler<GetDomainsCommand, GetDomainsResponse>
{
    public async Task<GetDomainsResponse> Handle(
        GetDomainsCommand command,
        CancellationToken cancellationToken = default)
    {
        var project = await projectRepository.GetProjectByIdWithDomainsAsync(
            command.ProjectId) ?? throw new ArgumentException($"Project with ID {command.ProjectId} not found.");

        var domainDtos = new List<GetDomainDto>();

        foreach (var domain in project.Domains!.Where(x => x.DeletedAtUtc == null))
        {
            var value = domain switch
            {
                ExternalDomain externalDomain => externalDomain.Value,
                InternalDomain internalDomain => internalDomain.Value,
                _ => string.Empty,
            };

            var isVerified = domain switch
            {
                ExternalDomain externalDomain => externalDomain.IsVerified,
                InternalDomain => true,
                _ => false
            };

            if (!string.IsNullOrEmpty(value))
            {
                domainDtos.Add(
                    new GetDomainDto(
                        domain.Id.Value,
                        domain.ServiceName,
                        domain.Protocol,
                        value,
                        domain is InternalDomain,
                        isVerified,
                        domain.IsPrimary));
            }
        }
        return new GetDomainsResponse(domainDtos);
    }
}
