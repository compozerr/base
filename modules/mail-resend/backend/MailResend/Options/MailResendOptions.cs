namespace MailResend.Options;

using System.ComponentModel.DataAnnotations;

public sealed class MailResendOptions
{
    [Required]
    public required string ApiKey { get; init; }
}