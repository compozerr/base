using Database.Models;

namespace Auth.Models;

public class UserRole : BaseEntityWithId<UserRoleId>
{
    public required UserId UserId { get; set; }
    public required RoleId RoleId { get; set; }
    public User User { get; set; } = default!;
    public Role Role { get; set; } = default!;
}