using Mail;

namespace Api.Emails;

public sealed class OrganizationInvitationTemplate : ReactEmailTemplate
{
    public OrganizationInvitationTemplate() : base("Emails/organization-invitation.html") { }

    public required string CompanyName { get; init; }
    public required string OrganizationName { get; init; }
    public required string InviterName { get; init; }
    public required string Role { get; init; }
    public required string AcceptLink { get; init; }
    public required string CompanyAddress { get; init; }
}
