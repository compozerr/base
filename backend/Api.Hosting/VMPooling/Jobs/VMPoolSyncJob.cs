using Api.Data;
using Api.Data.Repositories;
using Api.Hosting.VMPooling.Core;
using Core.Extensions;
using Jobs;
using Microsoft.EntityFrameworkCore;
using Serilog;

namespace Api.Hosting.VMPooling.Jobs;

public class VMPoolSyncJob(
    IVMPoolRepository vMPoolRepository,
    IVMPoolerFactory vMPoolerFactory) : JobBase<VMPoolSyncJob>
{
    private readonly ILogger _logger = Log.ForContext<VMPoolSyncJob>();

    public override async Task ExecuteAsync()
    {
        var pools = await vMPoolRepository.GetAllAsync();

        await pools.ApplyAsync(
            SyncPoolAsyncAndLogErrors);
    }

    private async Task SyncPoolAsyncAndLogErrors(VMPool pool)
    {
        try
        {
            await SyncPoolAsync(pool);
        }
        catch (Exception ex)
        {
            _logger.Error(
                ex,
                "Error syncing VMPool {VMPoolId}: {ErrorMessage}",
                pool.Id,
                ex.Message);
        }
    }

    private async Task SyncPoolAsync(VMPool pool)
    {
        var items = await vMPoolRepository.GetVMPoolItemsFromPoolAsync(
                        pool.Id,
                        CancellationToken.None);

        var itemCount = items.Count;

        _logger.Information(
            "VMPool {VMPoolId} has {ItemCount} available items.",
            pool.Id,
            itemCount);

        var vmPooler = vMPoolerFactory.CreateVMPooler(pool);
        if (itemCount < pool.InstanceCount)
        {
            var itemsToAdd = pool.InstanceCount - itemCount;

            _logger.Information(
                "VMPool {VMPoolId} needs {ItemsToAdd} more items. Adding them.",
                pool.Id,
                itemsToAdd);

            for (int i = 0; i < itemsToAdd; i++)
            {
                await CreateNewInstanceAndLogError(vmPooler);
            }

            _logger.Information(
                "Added {ItemsAdded} items to VMPool {VMPoolId}.",
                itemsToAdd,
                pool.Id);
        }

        await items.ApplyAsync(
            x => MakeSureVMPoolItemIsAliveAndLogError(vmPooler, x, CancellationToken.None));
    }

    private async Task MakeSureVMPoolItemIsAliveAndLogError(BaseVMPooler pooler, VMPoolItem item, CancellationToken cancellationToken)
    {
        try
        {
            await pooler.MakeSureInstanceIsAliveAsync(item, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.Error(
                ex,
                "Error making sure VM instance is alive for pool item {VMPoolItemId}: {ErrorMessage}",
                item.Id,
                ex.Message);
        }
    }

    private async Task CreateNewInstanceAndLogError(BaseVMPooler pooler)
    {
        try
        {
            await pooler.CreateNewInstanceAsync();
        }
        catch (Exception ex)
        {
            _logger.Error(
                ex,
                "Error creating new VM instance when syncing pool: {ErrorMessage}",
                ex.Message);
        }
    }
}