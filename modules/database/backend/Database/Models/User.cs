namespace Database.Models;

public class User : BaseEntity
{
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;

    // Navigation properties
    public virtual ICollection<UserRole> UserRoles { get; set; } = [];
}