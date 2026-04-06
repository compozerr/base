using Api.Data;
using Api.Data.Repositories;
using Core.MediatR;
using Microsoft.EntityFrameworkCore;

namespace Api.Endpoints.Projects.Domains.Get;

public sealed class GetDomainsCommandHandler(
    IProjectRepository projectRepository,
    ApiDbContext context) : ICommandHandler<GetDomainsCommand, GetDomainsResponse>
{
    public async Task<GetDomainsResponse> Handle(
        GetDomainsCommand command,
        CancellationToken cancellationToken = default)
    {
        var project = await projectRepository.GetProjectByIdWithDomainsAsync(
            command.ProjectId) ?? throw new ArgumentException($"Project with ID {command.ProjectId} not found.");

        var envNames = await context.Set<Data.ProjectEnvironment>()
            .Where(e => e.ProjectId == command.ProjectId)
            .ToDictionaryAsync(
                e => e.Id,
                e => e.Name ?? (e.Type == EnvironmentType.Production ? "Production" : e.Id.Value.ToString()),
                cancellationToken);

        var domainDtos = new List<GetDomainDto>();
        var seenValues = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

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

            if (!string.IsNullOrEmpty(value) && seenValues.Add(value))
            {
                string? envName = null;
                if (domain.ProjectEnvironmentId is { } envId)
                    envNames.TryGetValue(envId, out envName);

                domainDtos.Add(
                    new GetDomainDto(
                        domain.Id.Value,
                        domain.ServiceName,
                        domain.Protocol,
                        value,
                        domain is InternalDomain,
                        isVerified,
                        domain.IsPrimary,
                        domain.ProjectEnvironmentId?.Value,
                        envName));
            }
        }
        return new GetDomainsResponse(domainDtos);
    }
}
