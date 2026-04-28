using System.Text.Json.Serialization;
using Database.Models;

namespace Mail.Models;

public class SentEmail : BaseEntityWithId<SentEmailId>
{
    public required string FromAddress { get; set; }
    public string? FromDisplayName { get; set; }
    public required string ToAddress { get; set; }
    public string? ToDisplayName { get; set; }
    public required string Subject { get; set; }
    public required string HtmlBody { get; set; }
    public string? TemplateName { get; set; }
    public required DateTime SentAtUtc { get; set; }
    public required SentEmailStatus Status { get; set; }
    public string? ErrorMessage { get; set; }
}

[JsonConverter(typeof(JsonStringEnumConverter<SentEmailStatus>))]
public enum SentEmailStatus
{
    Sent = 0,
    Failed = 1
}
