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
    private readonly ApiDbContext _context = context;

    public async Task<InternalDomain> GetParentDomainAsync(DomainId domainId, CancellationToken cancellationToken = default)
    {
        var domain = await GetByIdAsync(
            domainId,
            cancellationToken) ?? throw new ArgumentException("Domain not found");

        if (domain is InternalDomain internalDomain)
            return internalDomain;

        var parentDomain = await _context.Domains.FirstOrDefaultAsync(
            x => x.ProjectId == domain.ProjectId && x.Type == DomainType.Internal && x.ServiceName == domain.ServiceName,
            cancellationToken) ?? throw new ArgumentException("Parent domain not found");

        return (InternalDomain)parentDomain;
    }

    public async Task<bool> IsProjectDomainUniqueAsync(ProjectId projectId, string domain)
    {
        var hasSome = await _context.Domains
                        .AsNoTracking()
                        .AnyAsync(d => d.ProjectId == projectId && d.Type == DomainType.External && ((ExternalDomain)d).Value == domain);

        return !hasSome;
    }
};