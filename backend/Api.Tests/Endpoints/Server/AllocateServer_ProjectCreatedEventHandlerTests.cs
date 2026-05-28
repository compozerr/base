using System.Security.Claims;
using Api.Abstractions;
using Api.Abstractions.Exceptions;
using Api.Data;
using Api.Data.Repositories;
using Api.Services.ServerVisibility;
using Auth.Abstractions;
using Cli.Abstractions;
using Cli.Endpoints.Projects;
using Core.Abstractions;
using Microsoft.AspNetCore.Http;
using NSubstitute;

namespace Api.Tests.Endpoints.Servers;

/// <summary>
/// Regression tests for the allocator's interaction with
/// <see cref="IServerVisibilityFilter"/>.
///
/// Spec A requires: non-admin callers must never be allocated an
/// <see cref="Api.Data.ServerVisibility.AdminOnly"/> server, even if it's the
/// only candidate in their location. Platform admins must still be allocated
/// an AdminOnly server when no Public alternative exists in their location.
/// </summary>
public class AllocateServer_ProjectCreatedEventHandlerTests
{
    private readonly IServerRepository _serverRepository = Substitute.For<IServerRepository>();

    private static IHttpContextAccessor AccessorFor(bool isAdmin)
    {
        var accessor = Substitute.For<IHttpContextAccessor>();
        var ctx = new DefaultHttpContext();
        var identity = new ClaimsIdentity(
            isAdmin ? new[] { new Claim(ClaimTypes.Role, "admin") } : Array.Empty<Claim>(),
            authenticationType: "test");
        ctx.User = new ClaimsPrincipal(identity);
        accessor.HttpContext.Returns(ctx);
        return accessor;
    }

    private static Api.Data.Server MakeServer(
        LocationId locationId,
        Api.Data.ServerVisibility visibility,
        decimal ramPercentage = 0m)
        => new()
        {
            Id = ServerId.CreateNew(),
            LocationId = locationId,
            SecretId = SecretId.CreateNew(),
            ServerVisibility = visibility,
            Usage = new ServerUsage { AvgRamPercentage = ramPercentage },
        };

    private async Task<Project> InvokeAllocateAsync(
        AllocateServer_ProjectCreatedEventHandler handler,
        Project project)
    {
        var envelope = new DomainEventEnvelope<ProjectCreatedEvent>(
            new ProjectCreatedEvent(project),
            DomainEventTiming.BeforeSaveChanges);
        await handler.Handle(envelope, CancellationToken.None);
        return project;
    }

    private static Project NewProjectInLocation(LocationId locationId) => new()
    {
        Name = "test",
        RepoUri = new Uri("https://github.com/test/repo"),
        UserId = UserId.CreateNew(),
        LocationId = locationId,
        State = ProjectState.Running,
        ServerTierId = new ServerTierId("T0"),
    };

    [Fact]
    public async Task NonAdmin_DoesNotGetAdminOnlyServer_EvenWhenItsTheOnlyCandidateInLocation()
    {
        var location = LocationId.CreateNew();
        var fallbackLocation = LocationId.CreateNew();
        var adminOnlyInLocation = MakeServer(location, Api.Data.ServerVisibility.AdminOnly);
        var publicElsewhere = MakeServer(fallbackLocation, Api.Data.ServerVisibility.Public);

        _serverRepository.GetServersByLocationId(location)
            .Returns(new List<Api.Data.Server> { adminOnlyInLocation });
        _serverRepository.GetAllAsync()
            .Returns(new List<Api.Data.Server> { adminOnlyInLocation, publicElsewhere });

        var handler = new AllocateServer_ProjectCreatedEventHandler(
            new ServerAllocator(_serverRepository, new ServerVisibilityFilter(AccessorFor(isAdmin: false))));

        var project = await InvokeAllocateAsync(handler, NewProjectInLocation(location));

        Assert.Equal(publicElsewhere.Id, project.ServerId);
        Assert.Equal(fallbackLocation, project.LocationId);
    }

    [Fact]
    public async Task PlatformAdmin_GetsAdminOnly_WhenNoPublicInLocation()
    {
        var location = LocationId.CreateNew();
        var adminOnly = MakeServer(location, Api.Data.ServerVisibility.AdminOnly);

        _serverRepository.GetServersByLocationId(location)
            .Returns(new List<Api.Data.Server> { adminOnly });
        _serverRepository.GetAllAsync()
            .Returns(new List<Api.Data.Server> { adminOnly });

        var handler = new AllocateServer_ProjectCreatedEventHandler(
            new ServerAllocator(_serverRepository, new ServerVisibilityFilter(AccessorFor(isAdmin: true))));

        var project = await InvokeAllocateAsync(handler, NewProjectInLocation(location));

        Assert.Equal(adminOnly.Id, project.ServerId);
        // Server is in the requested location, so the handler should not change LocationId.
        Assert.Equal(location, project.LocationId);
    }

    [Fact]
    public async Task NonAdmin_PrefersPublic_WhenBothPublicAndAdminOnlyExistInLocation()
    {
        var location = LocationId.CreateNew();
        var publicServer = MakeServer(location, Api.Data.ServerVisibility.Public, ramPercentage: 80m);
        var adminOnly = MakeServer(location, Api.Data.ServerVisibility.AdminOnly, ramPercentage: 10m);

        _serverRepository.GetServersByLocationId(location)
            .Returns(new List<Api.Data.Server> { publicServer, adminOnly });

        var handler = new AllocateServer_ProjectCreatedEventHandler(
            new ServerAllocator(_serverRepository, new ServerVisibilityFilter(AccessorFor(isAdmin: false))));

        var project = await InvokeAllocateAsync(handler, NewProjectInLocation(location));

        // Even though the AdminOnly server is less-loaded, non-admins must not see it.
        Assert.Equal(publicServer.Id, project.ServerId);
    }

    // Gate (d): when a non-admin's location has only Private + AdminOnly servers
    // (and none exist anywhere else), the filter excludes BOTH tiers — Private is
    // never returned to anyone, AdminOnly only to platform admins. The Codex finding
    // worded this as "assert Private is selected", but the IServerVisibilityFilter
    // contract is explicit that Private is never a fallback; selecting it for a
    // non-admin would itself be a visibility leak. The correct, secure behaviour is
    // a controlled failure — proving Private is NOT silently handed out.
    [Fact]
    public async Task NonAdmin_GetsControlledError_WhenOnlyPrivateAndAdminOnlyExist()
    {
        var location = LocationId.CreateNew();
        var privateInLocation = MakeServer(location, Api.Data.ServerVisibility.Private);
        var adminOnlyInLocation = MakeServer(location, Api.Data.ServerVisibility.AdminOnly);

        _serverRepository.GetServersByLocationId(location)
            .Returns(new List<Api.Data.Server> { privateInLocation, adminOnlyInLocation });
        _serverRepository.GetAllAsync()
            .Returns(new List<Api.Data.Server> { privateInLocation, adminOnlyInLocation });

        var handler = new AllocateServer_ProjectCreatedEventHandler(
            _serverRepository,
            new ServerVisibilityFilter(AccessorFor(isAdmin: false)));

        var ex = await Assert.ThrowsAsync<NoAvailableServerException>(
            () => InvokeAllocateAsync(handler, NewProjectInLocation(location)));

        // Controlled domain failure, not an InvalidOperationException from .First().
        Assert.IsNotType<InvalidOperationException>(ex);
        Assert.False(string.IsNullOrWhiteSpace(ex.Message));
    }

    // Gate (e): the "only AdminOnly servers exist for a non-admin" path — exactly the
    // empty-filtered-sequence path that previously threw an unhandled .First()
    // (InvalidOperationException → 5xx). Must now surface a controlled failure.
    [Fact]
    public async Task NonAdmin_GetsControlledError_WhenOnlyAdminOnlyExists()
    {
        var location = LocationId.CreateNew();
        var adminOnlyInLocation = MakeServer(location, Api.Data.ServerVisibility.AdminOnly);

        _serverRepository.GetServersByLocationId(location)
            .Returns(new List<Api.Data.Server> { adminOnlyInLocation });
        _serverRepository.GetAllAsync()
            .Returns(new List<Api.Data.Server> { adminOnlyInLocation });

        var handler = new AllocateServer_ProjectCreatedEventHandler(
            _serverRepository,
            new ServerVisibilityFilter(AccessorFor(isAdmin: false)));

        var ex = await Assert.ThrowsAsync<NoAvailableServerException>(
            () => InvokeAllocateAsync(handler, NewProjectInLocation(location)));

        Assert.IsNotType<InvalidOperationException>(ex);
        Assert.False(string.IsNullOrWhiteSpace(ex.Message));
    }

    [Fact]
    public async Task PicksLowestRamPercentage_AmongVisibleServers()
    {
        var location = LocationId.CreateNew();
        var loaded = MakeServer(location, Api.Data.ServerVisibility.Public, ramPercentage: 90m);
        var fresh = MakeServer(location, Api.Data.ServerVisibility.Public, ramPercentage: 10m);

        _serverRepository.GetServersByLocationId(location)
            .Returns(new List<Api.Data.Server> { loaded, fresh });

        var handler = new AllocateServer_ProjectCreatedEventHandler(
            new ServerAllocator(_serverRepository, new ServerVisibilityFilter(AccessorFor(isAdmin: false))));

        var project = await InvokeAllocateAsync(handler, NewProjectInLocation(location));

        Assert.Equal(fresh.Id, project.ServerId);
    }
}
