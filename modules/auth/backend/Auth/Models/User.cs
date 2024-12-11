using Database.Models;

namespace Auth.Models;

public class User : BaseEntityWithId<UserId>
{
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string AvatarUrl { get; set; } = string.Empty;

    public ICollection<string> Roles { get; set; } = [];
}