using Mail.Models;

namespace Mail.Endpoints.Admin.GetSentEmail;

public sealed record GetSentEmailResponse(
    Guid Id,
    string FromAddress,
    string? FromDisplayName,
    string ToAddress,
    string? ToDisplayName,
    string Subject,
    string HtmlBody,
    string? TemplateName,
    DateTime SentAtUtc,
    SentEmailStatus Status,
    string? ErrorMessage);
