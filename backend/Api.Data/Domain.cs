namespace Api.Data;

public class Domain : BaseEntityWithId<DomainId>
{
    public required ProjectId ProjectId { get; set; }
    public required ServerId ServerId { get; set; }

    public required DomainType Type { get; set; }

    public virtual Server? Server { get; set; }
}