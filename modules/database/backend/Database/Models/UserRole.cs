namespace Database.Models;

public class UserRole : BaseEntity
{
    public int UserId { get; set; }
    public int RoleId { get; set; }

    // Navigation properties
    public virtual User? User { get; set; }
    public virtual Role? Role { get; set; }
}