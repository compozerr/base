using Auth.Abstractions;
using Auth.Models;
using Database.Models;
using Github.Abstractions;

namespace Github.Model;

public class Installation : BaseEntityWithId<InstallationId>
{
    public required UserId UserId { get; set; }
    public required string AccessToken { get; set; }
}