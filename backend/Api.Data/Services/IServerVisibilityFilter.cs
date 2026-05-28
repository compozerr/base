namespace Api.Data.Services;

/// <summary>
/// Single source of truth for which <see cref="Server"/> rows the current
/// caller is allowed to see / be allocated to. Use this everywhere a query
/// or in-memory filter on <see cref="ServerVisibility"/> would otherwise live.
///
/// Visibility rules (Spec A):
///   - Non-admin sees only <see cref="ServerVisibility.Public"/>.
///   - Platform admin additionally sees <see cref="ServerVisibility.AdminOnly"/>.
///   - <see cref="ServerVisibility.Private"/> and <see cref="ServerVisibility.Unknown"/>
///     are never returned by this filter — matches the pre-existing
///     allocator / location-repository / seeder behaviour.
/// </summary>
public interface IServerVisibilityFilter
{
    bool IsCurrentUserPlatformAdmin();

    IQueryable<Server> ApplyForCurrentUser(IQueryable<Server> source);

    IEnumerable<Server> ApplyForCurrentUser(IEnumerable<Server> source);

    /// <summary>
    /// User-context-independent filter for bootstrap / seed code that runs
    /// outside an HTTP request scope. Returns only servers in the canonical
    /// "visible to all users" tier (<see cref="ServerVisibility.Public"/>).
    /// </summary>
    IQueryable<Server> WhereVisibleToAllUsers(IQueryable<Server> source);
}
