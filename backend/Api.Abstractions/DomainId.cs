namespace Api.Abstractions;

public sealed record DomainId : IdBase<DomainId>, IId<DomainId>
{
    public DomainId(Guid value) : base(value)
    {
    }

    public static DomainId Create(Guid value)
        => new(value);
}
