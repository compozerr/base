using Database.Models;

namespace Auth.Models;

public abstract class UserLogin : BaseEntityWithId<UserLoginId>
{
    public required UserId UserId { get; set; }
    public required Provider Provider { get; set; }
    public required string ProviderUserId { get; set; }
}