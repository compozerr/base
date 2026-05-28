using System.Security.Claims;
using Api.Abstractions;
using Api.Data;
using Api.Services.ServerVisibility;
using Microsoft.AspNetCore.Http;
using NSubstitute;

namespace Api.Tests.Services.ServerVisibility;

public class ServerVisibilityFilterTests
{
    private static Server MakeServer(Api.Data.ServerVisibility visibility)
        => new()
        {
            Id = ServerId.CreateNew(),
            SecretId = SecretId.CreateNew(),
            ServerVisibility = visibility,
            HostName = $"host-{visibility}",
        };

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

    [Fact]
    public void NonAdmin_SeesOnlyPublic()
    {
        var filter = new ServerVisibilityFilter(AccessorFor(isAdmin: false));
        var servers = new[]
        {
            MakeServer(Api.Data.ServerVisibility.Public),
            MakeServer(Api.Data.ServerVisibility.Private),
            MakeServer(Api.Data.ServerVisibility.AdminOnly),
            MakeServer(Api.Data.ServerVisibility.Unknown),
        };

        var visible = filter.ApplyForCurrentUser(servers).ToList();

        Assert.Single(visible);
        Assert.Equal(Api.Data.ServerVisibility.Public, visible[0].ServerVisibility);
    }

    [Fact]
    public void Admin_SeesPublicAndAdminOnly()
    {
        var filter = new ServerVisibilityFilter(AccessorFor(isAdmin: true));
        var servers = new[]
        {
            MakeServer(Api.Data.ServerVisibility.Public),
            MakeServer(Api.Data.ServerVisibility.Private),
            MakeServer(Api.Data.ServerVisibility.AdminOnly),
            MakeServer(Api.Data.ServerVisibility.Unknown),
        };

        var visible = filter.ApplyForCurrentUser(servers).ToList();

        Assert.Equal(2, visible.Count);
        Assert.Contains(visible, s => s.ServerVisibility == Api.Data.ServerVisibility.Public);
        Assert.Contains(visible, s => s.ServerVisibility == Api.Data.ServerVisibility.AdminOnly);
        Assert.DoesNotContain(visible, s => s.ServerVisibility == Api.Data.ServerVisibility.Private);
        Assert.DoesNotContain(visible, s => s.ServerVisibility == Api.Data.ServerVisibility.Unknown);
    }

    [Fact]
    public void NoHttpContext_DefaultsToNonAdmin()
    {
        var accessor = Substitute.For<IHttpContextAccessor>();
        accessor.HttpContext.Returns((HttpContext?)null);
        var filter = new ServerVisibilityFilter(accessor);

        var servers = new[]
        {
            MakeServer(Api.Data.ServerVisibility.Public),
            MakeServer(Api.Data.ServerVisibility.AdminOnly),
        };

        var visible = filter.ApplyForCurrentUser(servers).ToList();

        Assert.False(filter.IsCurrentUserPlatformAdmin());
        Assert.Single(visible);
        Assert.Equal(Api.Data.ServerVisibility.Public, visible[0].ServerVisibility);
    }

    [Fact]
    public void IsCurrentUserPlatformAdmin_ReflectsRoleClaim()
    {
        Assert.False(new ServerVisibilityFilter(AccessorFor(isAdmin: false)).IsCurrentUserPlatformAdmin());
        Assert.True(new ServerVisibilityFilter(AccessorFor(isAdmin: true)).IsCurrentUserPlatformAdmin());
    }
}
