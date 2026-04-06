namespace Mail.Services;

public interface IMailService
{
    Task SendEmailAsync(Email email);
}
