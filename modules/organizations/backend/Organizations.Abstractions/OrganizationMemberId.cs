namespace Organizations.Abstractions;

public sealed record OrganizationMemberId : IdBase<OrganizationMemberId>, IId<OrganizationMemberId>
{
    public OrganizationMemberId(Guid value) : base(value)
    {
    }

    public static OrganizationMemberId Create(Guid value)
        => new(value);
}
