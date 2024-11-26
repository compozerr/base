using System.ComponentModel.DataAnnotations;

namespace Auth.AuthProviders;

public sealed class GithubOptions
{
    [Required]
    public required string ClientId { get; init; }

    [Required]
    public required string ClientSecret { get; init; }
}