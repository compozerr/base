using Database.Models;

namespace Auth.Models;

public class User : BaseEntityWithId<UserId>
{
    public string Email { get; set; } = string.Empty;
    public string AvatarUrl { get; set; } = string.Empty;

    public virtual ICollection<UserRole> UserRoles { get; set; } = [];
}