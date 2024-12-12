using System.ComponentModel.DataAnnotations;

namespace Github.Options;

public sealed class GithubAppOptions
{
    [Required]
    public required string ClientId { get; init; }

    [Required]
    public required string ClientSecret { get; init; }
}