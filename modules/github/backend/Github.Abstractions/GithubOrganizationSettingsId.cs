namespace Github.Abstractions;

public sealed record GithubOrganizationSettingsId(Guid Value) : IdBase<GithubOrganizationSettingsId>(Value), IId<GithubOrganizationSettingsId>
{
    public static GithubOrganizationSettingsId Create(Guid value)
        => new(value);
}
