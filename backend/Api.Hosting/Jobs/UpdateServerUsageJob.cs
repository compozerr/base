using Api.Data.Repositories;
using Api.Hosting.Services;
using Jobs;
using Serilog;

namespace Api.Hosting.Jobs;

public class UpdateServerUsageJob(
    IServerRepository serverRepository,
    IHostingApiFactory hostingApiFactory) : JobBase<UpdateServerUsageJob>
{
    public override async Task ExecuteAsync()
    {
        var servers = await serverRepository.GetAllAsync();

        foreach (var server in servers)
        {
            var hostingApi = await hostingApiFactory.GetHostingApiAsync(server.Id);
            var serverUsage = await hostingApi.GetServerUsageAsync();

            Log.Logger.ForContext(nameof(serverUsage), serverUsage)
                      .Information("Updated server usage for server ID: {ServerId}", server.Id);

            if (serverUsage is null) continue;

            server.Usage = serverUsage;
            await serverRepository.UpdateAsync(server);
        }
    }
}