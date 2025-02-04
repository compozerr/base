using Api.Data;
using Microsoft.EntityFrameworkCore;

namespace Api.Repositories;

public interface IServerRepository
{
    public Task<Secret> AddNewServer(string hashedSecret);
    public Task UpdateServer(
        string secret,
        string isoCountryCode,
        string machineId,
        string ram,
        string vCpu,
        string ip);
}

public sealed class ServerRepository(
    ApiDbContext context) : IServerRepository
{
    public async Task<Secret> AddNewServer(string hashedSecret)
    {
        using var transaction = await context.Database.BeginTransactionAsync();

        var secret = new Secret
        {
            Value = hashedSecret
        };

        context.Secrets.Add(secret);
        await context.SaveChangesAsync();

        var server = new Server
        {
            Ip = string.Empty,
            LocationId = null!,
            MachineId = string.Empty,
            Ram = string.Empty,
            VCpu = string.Empty,
            SecretId = secret.Id
        };

        context.Servers.Add(server);
        await context.SaveChangesAsync();

        await transaction.CommitAsync();

        return secret;
    }

    public async Task UpdateServer(string hashedSecret, string isoCountryCode, string machineId, string ram, string vCpu, string ip)
    {
        var server = await context.Servers
                                  .Include(s => s.Secret)
                                  .Where(s => s.Secret!.Value == hashedSecret)
                                  .FirstOrDefaultAsync() ?? throw new ArgumentException("Server not found");
        server.Ip = ip;
        server.MachineId = machineId;
        server.Ram = ram;
        server.VCpu = vCpu;

        var location = await context.Locations.Where(l => l.IsoCountryCode == isoCountryCode)
                                              .FirstOrDefaultAsync();

        if (location is null)
        {
            location = new Location
            {
                IsoCountryCode = isoCountryCode
            };

            context.Locations.Add(location);
        }

        server.Location = location;

        await context.SaveChangesAsync();
    }
}