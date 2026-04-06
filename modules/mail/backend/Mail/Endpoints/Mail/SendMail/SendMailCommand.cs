using Core.MediatR;

namespace Mail.Endpoints.Mail.SendMail;

public sealed record SendMailCommand(
	string From,
	string? DisplayName,
	string To,
	string Subject,
	string HtmlBody) : ICommand;
