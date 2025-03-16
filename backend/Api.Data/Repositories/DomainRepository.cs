using Database.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Api.Data.Repositories;

public interface IDomainRepository : IGenericRepository<Domain, DomainId, ApiDbContext>
{
};

public sealed class DomainRepository(
    ApiDbContext context) : GenericRepository<Domain, DomainId, ApiDbContext>(context), IDomainRepository
{
    private readonly ApiDbContext _context = context;
};