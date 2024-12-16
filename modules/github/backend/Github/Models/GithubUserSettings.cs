using Auth.Abstractions;
using Database.Models;
using Github.Abstractions;

namespace Github.Models;

public sealed class GithubUserSettings : BaseEntityWithId<GithubUserSettingsId>
{
    public required UserId UserId { get; set; }
    public required string SelectedOrganization { get; set; }
}