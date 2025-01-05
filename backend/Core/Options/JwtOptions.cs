using System.ComponentModel.DataAnnotations;

namespace Core.Options;

public sealed class JwtOptions
{
    [Required]
    public required string Key { get; init; }

    [Required]
    public required string Issuer { get; init; }

    [Required]
    public required string Audience { get; init; }
}