using Mail;

namespace MailResend;

public static class EmailGuard
{
    public static void ThrowIfUnresolvedPlaceholders(string subject, string htmlBody)
    {
        var subjectHasPlaceholders = subject.Contains("{{") || subject.Contains("}}");
        var bodyHasPlaceholders = htmlBody.Contains("{{") || htmlBody.Contains("}}");

        if (subjectHasPlaceholders || bodyHasPlaceholders)
        {
            var location = (subjectHasPlaceholders, bodyHasPlaceholders) switch
            {
                (true, true) => "subject and body",
                (true, false) => "subject",
                _ => "body"
            };

            throw new UnresolvedPlaceholderException(
                $"Email contains unresolved placeholders '{{{{' or '}}}}' in {location}. " +
                $"Subject: '{subject}'. Email has been queued for manual retry in Hangfire.");
        }
    }
}
