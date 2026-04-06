using Serilog;

namespace Mail.Services;

public class MockMailService : IMailService
{
    ILogger _logger = Log.ForContext<MockMailService>();
    public Task SendEmailAsync(Email email)
    {
        _logger.Information(
            "[MAIL SKIPPED] Mock send email to {EmailAddress} with subject: {MailTitle}",
            email.To,
            email.Subject);

        return Task.CompletedTask;
    }
}
