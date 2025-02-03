using Auth.Abstractions;
using Auth.Models;

namespace Api.Data;

public class Deployment : BaseEntityWithId<DeploymentId>
{
    public required UserId UserId { get; set; }
    public required ProjectId ProjectId { get; set; }
    public required ServerId ServerId { get; set; }
    public required string CommitHash { get; set; }

    public virtual User? User { get; set; }
    public virtual Project? Project { get; set; }
    public virtual Server? Server { get; set; }
}