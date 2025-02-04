using System.ComponentModel.DataAnnotations;

namespace Api.Options;

public sealed class EncryptionOptions
{
    [Required]
    public required string Secret { get; init; }
}