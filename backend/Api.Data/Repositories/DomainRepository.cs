using System.Security.Cryptography.X509Certificates;
using Database.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Api.Data.Repositories;

public interface IDomainRepository : IGenericRepository<Domain, DomainId, ApiDbContext>
{
    Task<bool> IsProjectDomainUniqueAsync(ProjectId projectId, string domain);
    Task<InternalDomain> GetParentDomainAsync(DomainId domainId, CancellationToken cancellationToken);
};

public sealed class DomainRepository(
    ApiDbContext context) : GenericRepository<Domain, DomainId, ApiDbContext>(context), IDomainRepository
{
    public async Task<InternalDomain> GetParentDomainAsync(DomainId domainId, CancellationToken cancellationToken = default)
    {
        var domain = await GetByIdAsync(
            domainId,
            cancellationToken) ?? throw new ArgumentException("Domain not found");

        if (domain is InternalDomain internalDomain)
            return internalDomain;

        var parentDomain = await Query().Include(x => x.Project)
                                                 .ThenInclude(x => x!.Server)
                                                 .FirstOrDefaultAsync(
            x => x.ProjectId == domain.ProjectId && x.Type == DomainType.Internal && x.Port == domain.Port,
            cancellationToken) ?? throw new ArgumentException("Parent domain not found");

        return (InternalDomain)parentDomain;
    }

    public async Task<bool> IsProjectDomainUniqueAsync(ProjectId projectId, string domain)
    {
        var hasSome = await Query()
                        .AsNoTracking()
                        .AnyAsync(d => d.ProjectId == projectId && d.Type == DomainType.External && ((ExternalDomain)d).Value == domain);

        return !hasSome;
    }
};