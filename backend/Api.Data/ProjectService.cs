using System.ComponentModel.DataAnnotations;

namespace Api.Data;

public class ProjectService : BaseEntityWithId<ProjectServiceId>
{
    public required ProjectId ProjectId { get; set; }
    [MaxLength(255)]
    public required string Name { get; set; }
    [MaxLength(6)]
    public required string Port { get; set; }
    [MaxLength(20)]
    public required string Protocol { get; set; } = "http";
    public bool IsSystem { get; set; }

    public virtual Project? Project { get; set; }
}
