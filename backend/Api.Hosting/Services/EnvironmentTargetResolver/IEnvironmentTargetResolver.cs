using Api.Abstractions;
using Api.Data;

namespace Api.Hosting.Services.EnvironmentTargetResolver;

/// <summary>
/// Resolves a <see cref="ProjectEnvironmentId"/> (or a <see cref="Domain"/> /
/// <see cref="ProjectService"/>) to the hosting target (<see cref="Server"/> and
/// <see cref="IHostingApi"/>) that owns the env's VM after the per-env server
/// allocation migration. Owns the apex/project-level fallback rule (when a
/// Domain has no <see cref="Domain.ProjectEnvironmentId"/>, fall back to the
/// project's Production env's server). Owns the legacy hint fallback to
/// <see cref="Project.ServerId"/> during the transition window in which
/// <see cref="ProjectEnvironment.ServerId"/> may not yet be backfilled.
///
/// Visibility filtering (<c>ServerVisibility.AdminOnly</c>) is NOT applied here —
/// that's an authorization concern handled by the read endpoints before they call
/// the resolver. See follow-up TODO.
/// </summary>
public interface IEnvironmentTargetResolver
{
    Task<IHostingApi> GetHostingApiAsync(ProjectEnvironmentId environmentId, CancellationToken cancellationToken = default);
    Task<Server> GetServerAsync(ProjectEnvironmentId environmentId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Resolves the server that should serve a Domain. If the Domain has a
    /// <see cref="Domain.ProjectEnvironmentId"/>, that env's server. Otherwise
    /// (apex/project-level domain) the project's Production env's server.
    /// Falls back to <see cref="Project.ServerId"/> (legacy hint) when neither
    /// env-level lookup yields a server.
    /// </summary>
    Task<Server> GetServerForDomainAsync(DomainId domainId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Resolves the server for an env when the caller has only the env id and
    /// wants to tolerate a missing assignment (returns null instead of
    /// throwing). Used by paths that previously checked
    /// <c>project?.ServerId is { } serverId</c> before calling out.
    /// </summary>
    Task<Server?> TryGetServerAsync(ProjectEnvironmentId environmentId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns the distinct set of servers that host any env of this project
    /// (deduplicated by ServerId). Used by project-wide fan-out call sites
    /// (Delete, Stop, UpdateDomains). The Server entities are returned with
    /// their <see cref="Server.Location"/> nav loaded.
    /// </summary>
    Task<IReadOnlyList<Server>> GetDistinctServersForProjectAsync(ProjectId projectId, CancellationToken cancellationToken = default);

    /// <summary>
    /// As <see cref="GetDistinctServersForProjectAsync"/>, but materialised to
    /// hosting-api instances ready to call.
    /// </summary>
    Task<IReadOnlyList<IHostingApi>> GetDistinctHostingApisForProjectAsync(ProjectId projectId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Resolves the server for a <see cref="ProjectService"/>. Service is
    /// env-scoped via <see cref="ProjectService.ProjectEnvironmentId"/>; when
    /// null (legacy/project-wide service), falls back to the project's
    /// Production env's server.
    /// </summary>
    Task<Server> GetServerForServiceAsync(ProjectId projectId, string serviceName, CancellationToken cancellationToken = default);
}
