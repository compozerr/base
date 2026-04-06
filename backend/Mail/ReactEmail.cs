namespace Mail;

public class ReactEmail : Email
{
    private ReactEmail(
        EmailAddress from,
        IReadOnlyList<EmailAddress> to,
        string subject,
        string htmlBody)
        : base(from, to, subject, htmlBody)
    {

    }

    public static async Task<ReactEmail> CreateAsync(
        EmailAddress from,
        IReadOnlyList<EmailAddress> to,
        string subject,
        ReactEmailTemplate template)
    {
        var htmlBody = await template.GetHtmlBodyAsync();

        return new ReactEmail(
            from,
            to,
            subject,
            htmlBody);
    }
}