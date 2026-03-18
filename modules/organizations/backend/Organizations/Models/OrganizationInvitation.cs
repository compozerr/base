using Auth.Abstractions;
using Database.Models;
using Organizations.Abstractions;

namespace Organizations.Models;

public class OrganizationInvitation : BaseEntityWithId<OrganizationInvitationId>
{
    public required OrganizationId OrganizationId { get; set; }
    public required string Email { get; set; }
    public required OrganizationRole Role { get; set; }
    public required UserId InvitedByUserId { get; set; }
    public required InvitationStatus Status { get; set; }
    public required string Token { get; set; }
    public required DateTime ExpiresAtUtc { get; set; }

    public virtual Organization? Organization { get; set; }
}
