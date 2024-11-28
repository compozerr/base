using Auth.Abstractions;
using Database.Models;

namespace Auth.Models;

public class User : BaseEntity<UserId>
{
    public string Email { get; set; } = string.Empty;
    public string AvatarUrl { get; set; } = string.Empty;

    // Navigation properties
    public virtual ICollection<UserRole> UserRoles { get; set; } = [];
}