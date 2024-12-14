using Auth.Abstractions;
using Database.Models;
using Github.Abstractions;

namespace Github.Model;

public class Installation : BaseEntityWithId<InstallationId>
{
    public required UserId UserId { get; set; }
    public required string AccessToken { get; set; }
    public required string Scope { get; set; }
}