using Core.MediatR;
using Mail.Repositories;

namespace Mail.Endpoints.Admin.ListSentEmails;

public sealed class ListSentEmailsCommandHandler(
    ISentEmailRepository sentEmailRepository) : ICommandHandler<ListSentEmailsCommand, ListSentEmailsResponse>
{
    public async Task<ListSentEmailsResponse> Handle(
        ListSentEmailsCommand command,
        CancellationToken cancellationToken = default)
    {
        var (items, nextCursor) = await sentEmailRepository.ListAsync(
            command.Cursor,
            command.Limit,
            cancellationToken);

        var dtos = items.Select(e => new SentEmailListItemDto(
            e.Id.Value,
            e.ToAddress,
            e.ToDisplayName,
            e.Subject,
            e.TemplateName,
            e.SentAtUtc,
            e.Status)).ToList();

        return new ListSentEmailsResponse(dtos, nextCursor);
    }
}
