using Api.Abstractions;
using Api.Data;
using Auth.Abstractions;

namespace Api.Hosting.VMPooling.Core;

public interface IVMPoolItemDelegator
{
    Task<VMPoolItemId?> GetValidPoolItemIdOrDefaultAsync(
        ProjectId projectId,
        CancellationToken cancellationToken);

    Task DelegateAsync(VMPoolItemId vmPoolItemId, UserId userId);
}

public class VMPoolItemDelegator : IVMPoolItemDelegator
{
    public Task DelegateAsync(VMPoolItemId vmPoolItemId, UserId userId)
    {
        throw new NotImplementedException();
    }
}