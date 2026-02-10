using Database.Models;

namespace Auth.Models;

public class ApiToken : BaseEntityWithId<ApiTokenId>
{
    public required string Name { get; set; }
    public required UserId UserId { get; set; }
    public User? User { get; set; }
    public required string TokenHash { get; set; }
    public required string TokenPrefix { get; set; }
    public DateTime? ExpiresAtUtc { get; set; }
    public DateTime? LastUsedAtUtc { get; set; }
}
