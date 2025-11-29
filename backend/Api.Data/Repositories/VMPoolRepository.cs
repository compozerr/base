using System.Security.Cryptography.X509Certificates;
using Api.Hosting.VMPooling.Core.VMPoolItemLookup;
using Database.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Api.Data.Repositories;

public interface IVMPoolRepository : IGenericRepository<VMPool, VMPoolId, ApiDbContext>
{
    Task<VMPoolItemId> GetFirstVMPoolItemIdOrDefaultAsync(
        VMPoolItemLookupRequest request,
        CancellationToken cancellationToken);
};

public sealed class VMPoolRepository(
    ApiDbContext context) : GenericRepository<VMPool, VMPoolId, ApiDbContext>(context), IVMPoolRepository
{
    public Task<VMPoolItemId> GetFirstVMPoolItemIdOrDefaultAsync(
        VMPoolItemLookupRequest request,
        CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }
};