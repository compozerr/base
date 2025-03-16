using Api.Abstractions.Exceptions;
using Api.Data.Services;
using Database.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Api.Data.Repositories;

public interface IServerRepository : IGenericRepository<Server, ServerId, ApiDbContext>
{
    public Task<Secret> AddNewServer(string newSecret);
    public Task<Server> UpdateServer(
        string secret,
        string isoCountryCode,
        string machineId,
        string ram,
        string vCpu,
        string ip,
        string hostName,
        string apiDomain);
    public Task<List<Server>> GetServersByLocationId(LocationId locationId);

    public Task<Server?> GetServerOrDefaultByTokenAsync(string token);
}

public sealed class ServerRepository(
    ApiDbContext context,
    IHashService hashService) : GenericRepository<Server, ServerId, ApiDbContext>(context), IServerRepository
{
    private readonly ApiDbContext _context = context;
    public async Task<Secret> AddNewServer(string newSecret)
    {
        var secret = new Secret
        {
            Value = hashService.Hash(newSecret)
        };

        var server = new Server
        {
            SecretId = secret.Id,
            Secret = secret,
            ServerVisibility = ServerVisibility.Public,
        };

        _context.Servers.Add(server);
        await _context.SaveChangesAsync();

        return secret;
    }

    public Task<Server?> GetServerOrDefaultByTokenAsync(string secret)
        => _context.Servers.Include(x => x.Secret)
                           .SingleOrDefaultAsync(x => x.Secret != null && x.Secret.Value == hashService.Hash(secret));

    public Task<List<Server>> GetServersByLocationId(LocationId locationId)
        => _context.Servers.Where(s => s.LocationId == locationId).ToListAsync();

    public async Task<Server> UpdateServer(
        string secret,
        string isoCountryCode,
        string machineId,
        string ram,
        string vCpu,
        string ip,
        string hostName,
        string apiDomain)
    {
        var hashedSecret = hashService.Hash(secret);

        var server = await _context.Servers
                                  .Include(s => s.Secret)
                                  .Where(s => s.Secret!.Value == hashedSecret)
                                  .FirstOrDefaultAsync() ?? throw new ServerNotFoundException();
        server.Ip = ip;
        server.MachineId = machineId;
        server.Ram = ram;
        server.VCpu = vCpu;
        server.HostName = hostName;
        server.ApiDomain = apiDomain;

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