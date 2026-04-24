using Mail.Models;

namespace Mail.Endpoints.Admin.ListSentEmails;

public sealed record ListSentEmailsResponse(
    IReadOnlyList<SentEmailListItemDto> Items,
    DateTime? NextCursor);

public sealed record SentEmailListItemDto(
    Guid Id,
    string ToAddress,
    string? ToDisplayName,
    string Subject,
    string? TemplateName,
    DateTime SentAtUtc,
    SentEmailStatus Status);
