namespace Mail;

public class Email(
    EmailAddress From,
    IReadOnlyList<EmailAddress> To,
    string Subject,
    string HtmlBody)
{
    public EmailAddress From { get; } = From;
    public IReadOnlyList<EmailAddress> To { get; } = To;
    public string Subject { get; } = Subject;
    public string HtmlBody { get; } = HtmlBody;
};