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

        foreach (var domain in project.Domains!)
        {
            var value = domain switch
            {
                ExternalDomain externalDomain => externalDomain.Value,
                InternalDomain internalDomain => internalDomain.Value,
                _ => string.Empty,
            };

            if (!string.IsNullOrEmpty(value))
            {
                domainDtos.Add(
                    new GetDomainDto(
                        domain.ServiceName,
                        value));
            }
        }
        return new GetDomainsResponse(domainDtos);
    }
}
