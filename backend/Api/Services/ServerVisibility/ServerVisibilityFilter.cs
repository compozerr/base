using Api.Data;
using Api.Data.Services;
using Microsoft.AspNetCore.Http;

namespace Api.Services.ServerVisibility;

public sealed class ServerVisibilityFilter(IHttpContextAccessor httpContextAccessor) : IServerVisibilityFilter
{
    public const string PlatformAdminRole = "admin";

    public bool IsCurrentUserPlatformAdmin()
        => httpContextAccessor.HttpContext?.User?.IsInRole(PlatformAdminRole) ?? false;

    public IQueryable<Server> ApplyForCurrentUser(IQueryable<Server> source)
        => IsCurrentUserPlatformAdmin()
            ? source.Where(s => s.ServerVisibility == Data.ServerVisibility.Public
                                || s.ServerVisibility == Data.ServerVisibility.AdminOnly)
            : source.Where(s => s.ServerVisibility == Data.ServerVisibility.Public);

    public IEnumerable<Server> ApplyForCurrentUser(IEnumerable<Server> source)
        => IsCurrentUserPlatformAdmin()
            ? source.Where(s => s.ServerVisibility == Data.ServerVisibility.Public
                                || s.ServerVisibility == Data.ServerVisibility.AdminOnly)
            : source.Where(s => s.ServerVisibility == Data.ServerVisibility.Public);

    public IQueryable<Server> WhereVisibleToAllUsers(IQueryable<Server> source)
        => source.Where(s => s.ServerVisibility == Data.ServerVisibility.Public);
}
