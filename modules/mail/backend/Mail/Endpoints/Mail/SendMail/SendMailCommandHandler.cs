using Core.MediatR;
using Mail.Services;

namespace Mail.Endpoints.Mail.SendMail;

public sealed class SendMailCommandHandler(
	IMailService mailService) : ICommandHandler<SendMailCommand>
{
	public Task Handle(SendMailCommand command, CancellationToken cancellationToken = default)
	{
		var email = new Email(
			   new EmailAddress(
				   command.From,
				   command.DisplayName),
			   [command.To],
			   command.Subject,
			   command.HtmlBody);

		return mailService.SendEmailAsync(email);
	}
}
