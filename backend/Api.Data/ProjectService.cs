using System.ComponentModel.DataAnnotations;

namespace Api.Data;

public class ProjectService : BaseEntityWithId<ProjectServiceId>
{
    public required ProjectId ProjectId { get; set; }
    [MaxLength(255)]
    public required string Name { get; set; }
    [MaxLength(6)]
    public required string Port { get; set; }
    public required Protocol Protocol { get; set; } = Protocol.Http;
    public bool IsSystem { get; set; }

    public virtual Project? Project { get; set; }
}
