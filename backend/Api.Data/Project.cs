using Auth.Abstractions;

namespace Api.Data;

public class Project : BaseEntityWithId<ProjectId>
{
    public required string Name { get; set; }
    public required Uri RepoUri { get; set; }
    public required UserId UserId { get; set; }
    public required LocationId LocationId { get; set; }
    public required ProjectState State { get; set; }
    public ServerId? ServerId { get; set; }

    public virtual Server? Server { get; set; }
    public virtual ICollection<ProjectEnvironment>? ProjectEnvironments { get; set; }
    public virtual ICollection<Domain>? Domains { get; set; }
    public virtual ICollection<ProjectUsage>? ProjectUsages { get; set; }
}