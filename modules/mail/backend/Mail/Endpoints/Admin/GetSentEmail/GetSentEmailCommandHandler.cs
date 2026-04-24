using Core.MediatR;
using Mail.Repositories;

namespace Mail.Endpoints.Admin.GetSentEmail;

public sealed class GetSentEmailCommandHandler(
    ISentEmailRepository sentEmailRepository) : ICommandHandler<GetSentEmailCommand, GetSentEmailResponse?>
{
    public async Task<GetSentEmailResponse?> Handle(
        GetSentEmailCommand command,
        CancellationToken cancellationToken = default)
    {
        var entity = await sentEmailRepository.GetByIdAsync(command.Id, cancellationToken);
        if (entity is null)
            return null;

        return new GetSentEmailResponse(
            entity.Id.Value,
            entity.FromAddress,
            entity.FromDisplayName,
            entity.ToAddress,
            entity.ToDisplayName,
            entity.Subject,
            entity.HtmlBody,
            entity.TemplateName,
            entity.SentAtUtc,
            entity.Status,
            entity.ErrorMessage);
    }
}
