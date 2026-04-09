namespace MailResend.Jobs;

public sealed class FailedEmailData
{
    public required string FromAddress { get; init; }
    public string? FromDisplayName { get; init; }
    public required List<FailedEmailRecipient> To { get; init; }
    public required string Subject { get; init; }
    public required string HtmlBody { get; init; }
}

public sealed class FailedEmailRecipient
{
    public required string Address { get; init; }
    public string? DisplayName { get; init; }
}
