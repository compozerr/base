using Mail;
using MailResend.Services;
using Resend;
using Serilog;

namespace MailResend.Jobs;

public sealed class RetryFailedEmailJob(IResendService resendService)
{
    public async Task ExecuteAsync(FailedEmailData data)
    {
        EmailGuard.ThrowIfUnresolvedPlaceholders(data.Subject, data.HtmlBody);

        Log.Information(
            "Retrying previously failed email to {Recipients} with subject '{Subject}'",
            string.Join(", ", data.To.Select(t => t.Address)),
            data.Subject);

        await resendService.EmailSendAsync(new EmailMessage
        {
            From = new Resend.EmailAddress
            {
                DisplayName = data.FromDisplayName,
                Email = data.FromAddress
            },
            To = [.. data.To.Select(to => new Resend.EmailAddress
            {
                DisplayName = to.DisplayName,
                Email = to.Address
            })],
            Subject = data.Subject,
            HtmlBody = data.HtmlBody
        });
    }
}
