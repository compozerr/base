using Core.Abstractions;
using Mail.Events;
using Mail.Models;
using Mail.Repositories;
using Serilog;

namespace Mail.EventHandlers;

public sealed class TrackSentEmailHandler(
    ISentEmailRepository sentEmailRepository) : IEventHandler<SendEmailEvent>
{
    private static readonly ILogger _logger = Log.ForContext<TrackSentEmailHandler>();

    public async Task Handle(SendEmailEvent notification, CancellationToken cancellationToken)
    {
        var email = notification.Email;
        var primaryRecipient = email.To.FirstOrDefault();

        if (primaryRecipient is null)
            return;

        try
        {
            var record = new SentEmail
            {
                FromAddress = email.From.Address,
                FromDisplayName = email.From.DisplayName,
                ToAddress = primaryRecipient.Address,
                ToDisplayName = primaryRecipient.DisplayName,
                Subject = email.Subject,
                HtmlBody = email.HtmlBody,
                TemplateName = email.TemplateName,
                SentAtUtc = DateTime.UtcNow,
                Status = SentEmailStatus.Sent
            };

            await sentEmailRepository.AddAsync(record, cancellationToken);
        }
        catch (Exception ex)
        {
            // Tracking must never break email dispatch. Log and swallow so other handlers (the
            // real send) still run.
            _logger.Error(ex, "Failed to persist sent-email record for {Subject}", email.Subject);
        }
    }
}
