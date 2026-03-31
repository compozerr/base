using System.ComponentModel.DataAnnotations;

namespace Api.Data;

public class ProjectEnvironment : BaseEntityWithId<ProjectEnvironmentId>
{
    public required ProjectId ProjectId { get; set; }
    public required List<string> Branches { get; set; }
    public required bool AutoDeploy { get; set; } = true;

    [MaxLength(255)]
    public string? Name { get; set; }

    public EnvironmentType Type { get; set; } = EnvironmentType.Production;

    /// <summary>
    /// For preview environments created from a pull request.
    /// </summary>
    public int? PullRequestNumber { get; set; }

    /// <summary>
    /// The generated subdomain for preview environments (e.g., "pr-42-myproject").
    /// </summary>
    [MaxLength(255)]
    public string? Subdomain { get; set; }

    public virtual Project? Project { get; set; }
    public virtual ICollection<ProjectEnvironmentVariable>? ProjectEnvironmentVariables { get; set; }
    public virtual ICollection<Deployment>? Deployments { get; set; }
}