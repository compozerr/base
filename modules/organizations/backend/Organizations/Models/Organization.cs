using Auth.Abstractions;
using Database.Models;
using Organizations.Abstractions;

namespace Organizations.Models;

public class Organization : BaseEntityWithId<OrganizationId>
{
    public required string Name { get; set; }
    public required string Slug { get; set; }
    public string? AvatarUrl { get; set; }
    public bool IsPersonal { get; set; }
    public required UserId OwnerUserId { get; set; }

    public virtual ICollection<OrganizationMember>? Members { get; set; }
    public virtual ICollection<OrganizationInvitation>? Invitations { get; set; }
}
