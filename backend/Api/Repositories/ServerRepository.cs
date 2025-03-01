using Api.Abstractions;
using Api.Data;
using Api.Endpoints.Server;
using Database.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Api.Repositories;

public interface IServerRepository : IGenericRepository<Server, ServerId, ApiDbContext>
{
    public Task<Secret> AddNewServer(string hashedSecret);
    public Task<Server> UpdateServer(
        string secret,
        string isoCountryCode,
        string machineId,
        string ram,
        string vCpu,
        string ip);
}

public sealed class ServerRepository(
    ApiDbContext context) : GenericRepository<Server, ServerId, ApiDbContext>(context), IServerRepository
{
    private readonly ApiDbContext _context = context;
    public async Task<Secret> AddNewServer(string hashedSecret)
    {
        var secret = new Secret
        {
            Value = hashedSecret
        };

        var server = new Server
        {
            SecretId = secret.Id,
            Secret = secret
        };

        _context.Servers.Add(server);
        await _context.SaveChangesAsync();

        return secret;
    }

    public async Task<Server> UpdateServer(string hashedSecret, string isoCountryCode, string machineId, string ram, string vCpu, string ip)
    {
        var server = await _context.Servers
                                  .Include(s => s.Secret)
                                  .Where(s => s.Secret!.Value == hashedSecret)
                                  .FirstOrDefaultAsync() ?? throw new ServerNotFoundException();
        server.Ip = ip;
        server.MachineId = machineId;
        server.Ram = ram;
        server.VCpu = vCpu;

        var location = await _context.Locations.Where(l => l.IsoCountryCode == isoCountryCode)
                                              .FirstOrDefaultAsync();

        if (location is null)
        {
            location = new Location
            {
                IsoCountryCode = isoCountryCode
            };

            _context.Locations.Add(location);
        }

        server.Location = location;

        await _context.SaveChangesAsync();

        return server;
    }
}