using System.ComponentModel.DataAnnotations;

namespace Api.Data;

public class Environment : BaseEntityWithId<EnvironmentId>
{
    public required ProjectId ProjectId { get; set; }
    public required EnvironmentType Type { get; set; }

    [MaxLength(100)]
    public required string Name { get; set; }

    [MaxLength(255)]
    public required string Branch { get; set; }

    public required EnvironmentStatus Status { get; set; }

    [MaxLength(255)]
    public string? VmIdentifier { get; set; }

    [MaxLength(255)]
    public string? Subdomain { get; set; }

    public bool AutoDestroy { get; set; } = false;

    public int? TtlDays { get; set; }

    public virtual Project? Project { get; set; }
    public virtual ICollection<Domain>? Domains { get; set; }
}
