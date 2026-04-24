namespace Mail;

public class Email(
    EmailAddress From,
    IReadOnlyList<EmailAddress> To,
    string Subject,
    string HtmlBody,
    string? TemplateName = null)
{
    public EmailAddress From { get; } = From;
    public IReadOnlyList<EmailAddress> To { get; } = To;
    public string Subject { get; } = Subject;
    public string HtmlBody { get; } = HtmlBody;
    public string? TemplateName { get; } = TemplateName;
};
