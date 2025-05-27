using System.ComponentModel.DataAnnotations;

namespace Github.Options;

public sealed class GithubAppOptions
{
    [Required]
    public required string AppId { get; init; }

    [Required]
    public required string PrivateKeyCertificateBase64 { get; init; }

    [Required]
    public required string WebhookSecret { get; init; }
}