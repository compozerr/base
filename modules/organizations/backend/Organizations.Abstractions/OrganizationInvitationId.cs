namespace Organizations.Abstractions;

public sealed record OrganizationInvitationId : IdBase<OrganizationInvitationId>, IId<OrganizationInvitationId>
{
    public OrganizationInvitationId(Guid value) : base(value)
    {
    }

    public static OrganizationInvitationId Create(Guid value)
        => new(value);
}
