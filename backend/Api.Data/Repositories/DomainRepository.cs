using Database.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Api.Data.Repositories;

public interface IDomainRepository : IGenericRepository<Domain, DomainId, ApiDbContext>
{
    Task<bool> IsProjectDomainUniqueAsync(ProjectId projectId, string domain);
};

public sealed class DomainRepository(
    ApiDbContext context) : GenericRepository<Domain, DomainId, ApiDbContext>(context), IDomainRepository
{
    private readonly ApiDbContext _context = context;

    public async Task<bool> IsProjectDomainUniqueAsync(ProjectId projectId, string domain)
    {
        var hasSome = await _context.Domains
                        .AsNoTracking()
                        .AnyAsync(d => d.ProjectId == projectId && d.Type == DomainType.External && ((ExternalDomain)d).Value == domain);

        return !hasSome;
    }
};