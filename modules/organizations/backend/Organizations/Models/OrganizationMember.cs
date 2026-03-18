using Auth.Abstractions;
using Database.Models;
using Organizations.Abstractions;

namespace Organizations.Models;

public class OrganizationMember : BaseEntityWithId<OrganizationMemberId>
{
    public required OrganizationId OrganizationId { get; set; }
    public required UserId UserId { get; set; }
    public required OrganizationRole Role { get; set; }

    public virtual Organization? Organization { get; set; }
}
