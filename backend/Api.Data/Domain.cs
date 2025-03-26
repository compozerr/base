namespace Api.Data;

public class Domain : BaseEntityWithId<DomainId>
{
    public required ProjectId ProjectId { get; set; }
    public DomainType Type { get; set; }

    public required string ServiceName { get; set; }
    public required string Port { get; set; }
    public bool IsPrimary { get; set; } = false;

    public virtual Project? Project { get; set; }

    public virtual string GetValue => string.Empty;
}