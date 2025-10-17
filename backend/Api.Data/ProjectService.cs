namespace Api.Data;

public class ProjectService : BaseEntityWithId<ProjectServiceId>
{
    public required ProjectId ProjectId { get; set; }
    public required string Name { get; set; }
    public required string Port { get; set; }
    public required string Protocol { get; set; } = "http";
    public bool IsSystem { get; set; }

    public virtual Project? Project { get; set; }
}
