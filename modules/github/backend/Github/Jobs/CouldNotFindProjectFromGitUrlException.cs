namespace Github.Jobs;

public sealed class CouldNotFindProjectFromGitUrlException(
    Uri GitUrl,
    string? Message = null) : Exception(
        Message ?? $"Could not find a project with the Git URL '{GitUrl}'.")
{
    public override string ToString()
        => $"{base.ToString()}, GitUrl: {GitUrl}";
}