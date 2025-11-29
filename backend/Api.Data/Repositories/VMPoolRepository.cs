using Database.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Api.Data.Repositories;

public interface IVMPoolRepository : IGenericRepository<VMPool, VMPoolId, ApiDbContext>
{
    Task<VMPoolItemId?> GetFirstVMPoolItemIdOrDefaultAsync(
        VMPoolItemLookupRequest request,
        CancellationToken cancellationToken);
};

public sealed class VMPoolRepository(
    ApiDbContext context) : GenericRepository<VMPool, VMPoolId, ApiDbContext>(context), IVMPoolRepository
{
    public Task<VMPoolItemId?> GetFirstVMPoolItemIdOrDefaultAsync(
        VMPoolItemLookupRequest request,
        CancellationToken cancellationToken)
    => Query()
        .Include(vmp => vmp.Server)
        .Include(vmp => vmp.VMPoolItems)
        .Where(vmp => vmp.Server != null && vmp.Server.LocationId == request.LocationId &&
                        vmp.ServerTierId == request.ServerTierId.Value &&
                        vmp.ProjectType == request.ProjectType)
        .Where(vmp => vmp.VMPoolItems != null)
        .SelectMany(vmp => vmp.VMPoolItems!.Where(vmpi => vmpi.DelegatedAt == null))
        .OrderBy(vmpi => vmpi.Id)
        .Select(vmpi => vmpi.Id)
        .FirstOrDefaultAsync(cancellationToken);
}
