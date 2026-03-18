namespace Organizations.Abstractions;

public sealed record OrganizationId : IdBase<OrganizationId>, IId<OrganizationId>
{
    public OrganizationId(Guid value) : base(value)
    {
    }

    public static OrganizationId Create(Guid value)
        => new(value);
}
