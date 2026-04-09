using Database.Models;
using Github.Abstractions;
using Organizations.Abstractions;

namespace Github.Models;

public sealed class GithubOrganizationSettings : BaseEntityWithId<GithubOrganizationSettingsId>
{
    public required OrganizationId OrganizationId { get; set; }
    public required string? SelectedProjectsInstallationId { get; set; }
    public required string? SelectedModulesInstallationId { get; set; }
}
