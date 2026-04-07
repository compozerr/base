using Api.Data;
using Api.Endpoints.Projects.Domains.Get;
using Core.MediatR;
using Microsoft.EntityFrameworkCore;

namespace Api.Endpoints.Projects.Environments.Domains;

public sealed class GetEnvironmentDomainsCommandHandler(
    ApiDbContext context) : ICommandHandler<GetEnvironmentDomainsCommand, GetDomainsResponse>
{
    public async Task<GetDomainsResponse> Handle(
        GetEnvironmentDomainsCommand command,
        CancellationToken cancellationToken = default)
    {
        var environment = await context.Set<Data.ProjectEnvironment>()
            .FirstOrDefaultAsync(e => e.Id == command.EnvironmentId, cancellationToken)
            ?? throw new ArgumentException("Environment not found");

        var domains = await context.Domains
            .Include(d => d.Project).ThenInclude(p => p!.Server)
            .Where(d => d.ProjectId == command.ProjectId
                     && d.DeletedAtUtc == null
                     && d.ProjectEnvironmentId == command.EnvironmentId)
            .ToListAsync(cancellationToken);

        var envName = environment.Name
            ?? (environment.Type == EnvironmentType.Production ? "Production" : environment.Id.Value.ToString());

        var domainDtos = new List<GetDomainDto>();
        var seenValues = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var domain in domains)
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
                domainDtos.Add(
                    new GetDomainDto(
                        domain.Id.Value,
                        domain.ServiceName,
                        domain.Protocol,
                        value,
                        domain is InternalDomain,
                        isVerified,
                        domain.IsPrimary,
                        domain.ProjectEnvironmentId?.Value ?? command.EnvironmentId.Value,
                        envName));
            }
        }

        return new GetDomainsResponse(domainDtos);
    }
}
