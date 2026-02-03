using System.ComponentModel.DataAnnotations;
using Api.Abstractions;
using Auth.Abstractions;

namespace Api.Data;

public class Project : BaseEntityWithId<ProjectId>
{
    [MaxLength(255)]
    public required string Name { get; set; }
    public required Uri RepoUri { get; set; }
    public required UserId UserId { get; set; }
    public required LocationId LocationId { get; set; }
    public required ProjectState State { get; set; }
    public required ServerTierId ServerTierId { get; set; }
    public ProjectType Type { get; set; } = ProjectType.Compozerr;

    /// <summary>
    /// GitHub App Installation ID for this project's repository.
    /// Used to generate installation tokens for git operations (more reliable than OAuth tokens).
    /// </summary>
    public long? GithubInstallationId { get; set; }

    public ServerId? ServerId { get; set; }
    public virtual Server? Server { get; set; }
    public virtual ICollection<ProjectEnvironment>? ProjectEnvironments { get; set; }
    public virtual ICollection<Domain>? Domains { get; set; }
    public virtual ICollection<ProjectUsage>? ProjectUsages { get; set; }
    public virtual ICollection<ProjectService>? ProjectServices { get; set; }
}