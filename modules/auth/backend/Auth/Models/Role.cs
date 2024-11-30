using Database.Models;

namespace Auth.Models;

public class Role : BaseEntityWithId<RoleId>
{
    public string? Name { get; set; }
    public string? Description { get; set; }
}
