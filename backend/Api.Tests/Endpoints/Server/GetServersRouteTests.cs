using System.Security.Claims;
using Api.Abstractions;
using Api.Data;
using Api.Data.Repositories;
using Api.Data.Services;
using Api.Endpoints.Server;
using Api.Services.ServerVisibility;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using NSubstitute;

namespace Api.Tests.Endpoints.Servers;

/// <summary>
/// Regression tests for <see cref="GetServersRoute"/>: must apply
/// <see cref="IServerVisibilityFilter"/> so non-admin callers never see
/// AdminOnly servers, while platform admins do.
/// </summary>
public class GetServersRouteTests : IDisposable
{
    private sealed class TestApiDbContext(DbContextOptions<ApiDbContext> options, IMediator mediator)
        : ApiDbContext(options, mediator)
    {
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.Entity<ModuleRegistryEntry>().Ignore(e => e.Compatibility);
            modelBuilder.Entity<Api.Data.Server>().Ignore(e => e.Usage);
        }
    }

    static GetServersRouteTests() => ApiDbContextTestBootstrap.EnsureInitialized();

    private readonly ApiDbContext _context;
    private readonly ServerRepository _repository;

    public GetServersRouteTests()
    {
        var options = new DbContextOptionsBuilder<ApiDbContext>()
            .UseInMemoryDatabase(databaseName: $"get-servers-{Guid.NewGuid()}")
            .Options;

        _context = new TestApiDbContext(options, Substitute.For<IMediator>());
        _repository = new ServerRepository(_context, new HashService());
    }

    public void Dispose() => _context.Dispose();

    private static IServerVisibilityFilter FilterFor(bool isAdmin)
    {
        var accessor = Substitute.For<IHttpContextAccessor>();
        var ctx = new DefaultHttpContext();
        var identity = new ClaimsIdentity(
            isAdmin ? new[] { new Claim(ClaimTypes.Role, "admin") } : Array.Empty<Claim>(),
            authenticationType: "test");
        ctx.User = new ClaimsPrincipal(identity);
        accessor.HttpContext.Returns(ctx);
        return new ServerVisibilityFilter(accessor);
    }

    private async Task<(Api.Data.Server publicServer, Api.Data.Server adminOnly, Api.Data.Server privateServer)> SeedServersAsync()
    {
        var publicServer = new Api.Data.Server
        {
            Id = ServerId.CreateNew(),
            SecretId = SecretId.CreateNew(),
            ServerVisibility = Api.Data.ServerVisibility.Public,
            HostName = "public-host",
            ApiDomain = "api.public.example.com",
            Ip = "10.0.0.1",
        };
        var adminOnly = new Api.Data.Server
        {
            Id = ServerId.CreateNew(),
            SecretId = SecretId.CreateNew(),
            ServerVisibility = Api.Data.ServerVisibility.AdminOnly,
            HostName = "admin-only-host",
            ApiDomain = "api.admin-only.example.com",
            Ip = "10.0.0.2",
        };
        var privateServer = new Api.Data.Server
        {
            Id = ServerId.CreateNew(),
            SecretId = SecretId.CreateNew(),
            ServerVisibility = Api.Data.ServerVisibility.Private,
            HostName = "private-host",
            ApiDomain = "api.private.example.com",
            Ip = "10.0.0.3",
        };

        _context.Servers.AddRange(publicServer, adminOnly, privateServer);
        await _context.SaveChangesAsync();
        return (publicServer, adminOnly, privateServer);
    }

    [Fact]
    public async Task NonAdmin_DoesNotSeeAdminOnlyServer()
    {
        var (publicServer, adminOnly, privateServer) = await SeedServersAsync();

        var result = await GetServersRoute.ExecuteAsync(_repository, FilterFor(isAdmin: false), CancellationToken.None);

        var ids = result.Value!.Servers.Select(s => s.Id).ToHashSet();
        Assert.Contains(publicServer.Id, ids);
        Assert.DoesNotContain(adminOnly.Id, ids);
        Assert.DoesNotContain(privateServer.Id, ids);
        Assert.Single(result.Value.Servers);
    }

    [Fact]
    public async Task PlatformAdmin_SeesPublicAndAdminOnly_ButNotPrivate()
    {
        var (publicServer, adminOnly, privateServer) = await SeedServersAsync();

        var result = await GetServersRoute.ExecuteAsync(_repository, FilterFor(isAdmin: true), CancellationToken.None);

        var ids = result.Value!.Servers.Select(s => s.Id).ToHashSet();
        Assert.Contains(publicServer.Id, ids);
        Assert.Contains(adminOnly.Id, ids);
        Assert.DoesNotContain(privateServer.Id, ids);
        Assert.Equal(2, result.Value.Servers.Count);
    }

    [Fact]
    public async Task ExcludesSoftDeletedServers()
    {
        var (publicServer, _, _) = await SeedServersAsync();
        publicServer.DeletedAtUtc = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        var result = await GetServersRoute.ExecuteAsync(_repository, FilterFor(isAdmin: true), CancellationToken.None);

        Assert.DoesNotContain(result.Value!.Servers, s => s.Id == publicServer.Id);
    }

    [Fact]
    public async Task ProjectsLocationIsoCountryCode_WhenServerHasLocation()
    {
        var location = new Location { IsoCountryCode = "deu" };
        _context.Locations.Add(location);
        await _context.SaveChangesAsync();

        var server = new Api.Data.Server
        {
            Id = ServerId.CreateNew(),
            SecretId = SecretId.CreateNew(),
            LocationId = location.Id,
            ServerVisibility = Api.Data.ServerVisibility.Public,
            HostName = "host",
            ApiDomain = "api",
            Ip = "1.2.3.4",
        };
        _context.Servers.Add(server);
        await _context.SaveChangesAsync();

        var result = await GetServersRoute.ExecuteAsync(_repository, FilterFor(isAdmin: false), CancellationToken.None);

        var dto = Assert.Single(result.Value!.Servers);
        Assert.Equal(server.Id, dto.Id);
        Assert.Equal(location.Id, dto.LocationId);
        Assert.Equal("deu", dto.LocationIsoCountryCode);
    }
}
