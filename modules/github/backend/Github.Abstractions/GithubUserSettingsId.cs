namespace Github.Abstractions;

public sealed record GithubUserSettingsId(Guid Value) : IdBase<GithubUserSettingsId>(Value), IId<GithubUserSettingsId>
{
    public static GithubUserSettingsId Create(Guid value)
        => new(value);
}
