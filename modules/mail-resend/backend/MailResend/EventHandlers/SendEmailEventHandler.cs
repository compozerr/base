using Core.Abstractions;
using Core.Extensions;
using Hangfire;
using Mail;
using Mail.Events;
using MailResend.Jobs;
using MailResend.Services;
using Serilog;

namespace MailResend.EventHandlers;

public sealed class SendEmailEventHandler(IResendService resendService) : IEventHandler<SendEmailEvent>
{
    public async Task Handle(SendEmailEvent notification, CancellationToken cancellationToken)
    {
        var email = notification.Email;

        if (ContainsUnresolvedPlaceholders(email))
        {
            var failedEmailData = new FailedEmailData
            {
                FromAddress = email.From.Address,
                FromDisplayName = email.From.DisplayName,
                To = email.To.Select(to => new FailedEmailRecipient
                {
                    Address = to.Address,
                    DisplayName = to.DisplayName
                }).ToList(),
                Subject = email.Subject,
                HtmlBody = email.HtmlBody
            };

            var jobId = BackgroundJob.Enqueue<RetryFailedEmailJob>(
                job => job.ExecuteAsync(failedEmailData));

            Log.Fatal(
                "Email to {Recipients} contains unresolved placeholders '{{{{' or '}}}}'. " +
                "Email was NOT sent. Queued as Hangfire job {JobId} for manual retry after fixing the template",
                string.Join(", ", email.To.Select(t => t.Address)),
                jobId);

            throw new UnresolvedPlaceholderException(
                $"Email contains unresolved placeholders '{{{{' or '}}}}'. " +
                $"Subject: '{email.Subject}'. Queued as Hangfire job {jobId} for manual retry.");
        }

        var result = await resendService.EmailSendAsync(new Resend.EmailMessage
        {
            From = new Resend.EmailAddress
            {
                DisplayName = email.From.DisplayName,
                Email = email.From.Address
            },
            To = [.. email.To.Select(to => new Resend.EmailAddress
            {
                DisplayName = to.DisplayName,
                Email = to.Address
            })],
            Subject = email.Subject,
            HtmlBody = email.HtmlBody
        }, cancellationToken).LogExceptionAsError();
    }

    private static bool ContainsUnresolvedPlaceholders(Email email)
        => email.Subject.Contains("{{") || email.Subject.Contains("}}")
        || email.HtmlBody.Contains("{{") || email.HtmlBody.Contains("}}");
}
