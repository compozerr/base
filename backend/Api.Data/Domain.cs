using System.ComponentModel.DataAnnotations;

namespace Api.Data;

public class Domain : BaseEntityWithId<DomainId>
{
    public required ProjectId ProjectId { get; set; }
    public DomainType Type { get; set; }

    [MaxLength(255)]
    public required string ServiceName { get; set; }
    [MaxLength(6)]
    public required string Port { get; set; }
    public required Protocol Protocol { get; set; } = Protocol.Http;
    public bool IsPrimary { get; set; } = false;

    public virtual Project? Project { get; set; }

    public virtual string GetValue => string.Empty;
}