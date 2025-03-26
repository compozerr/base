using Auth.Abstractions;
using Auth.Models;
using Microsoft.Build.Tasks;

namespace Api.Data;

public class Deployment : BaseEntityWithId<DeploymentId>
{
    public required UserId UserId { get; set; }
    public required ProjectId ProjectId { get; set; }
    public required string CommitHash { get; set; }
    public required string CommitMessage { get; set; }
    public required string CommitAuthor { get; set; }
    public required string CommitBranch { get; set; }
    public required string CommitEmail { get; set; }
    
    public required DeploymentStatus Status { get; set; }

    public TimeSpan? BuildDuration { get; set; }

    public virtual User? User { get; set; }
    public virtual Project? Project { get; set; }
}