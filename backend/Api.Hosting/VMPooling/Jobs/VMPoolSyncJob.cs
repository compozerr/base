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
        var pools = await vMPoolRepository.GetAllAsync(x => x.Include(x => x.Server));

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
        var itemCount = await vMPoolRepository.GetVMPoolItemCountFromPoolAsync(
                        pool.Id,
                        CancellationToken.None);

        _logger.Information(
            "VMPool {VMPoolId} has {ItemCount} available items.",
            pool.Id,
            itemCount);

        if (itemCount < pool.InstanceCount)
        {
            var vmPooler = vMPoolerFactory.CreateVMPooler(pool);

            var itemsToAdd = pool.InstanceCount - itemCount;

            _logger.Information(
                "VMPool {VMPoolId} needs {ItemsToAdd} more items. Adding them.",
                pool.Id,
                itemsToAdd);

            var tasks = new List<Task>();

            for (int i = 0; i < itemsToAdd; i++)
            {
                tasks.Add(CreateNewInstanceAndLogError(vmPooler));
            }

            await Task.WhenAll(tasks);

            _logger.Information(
                "Added {ItemsAdded} items to VMPool {VMPoolId}.",
                itemsToAdd,
                pool.Id);
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