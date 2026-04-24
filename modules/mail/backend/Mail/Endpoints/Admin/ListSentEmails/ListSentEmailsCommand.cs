using Core.MediatR;

namespace Mail.Endpoints.Admin.ListSentEmails;

public sealed record ListSentEmailsCommand(
    DateTime? Cursor = null,
    int Limit = 50) : ICommand<ListSentEmailsResponse>;
