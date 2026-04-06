using Core.Abstractions;
using Core.Extensions;
using Mail.Events;
using MailResend.Services;

namespace MailResend.EventHandlers;

public sealed class SendEmailEventHandler(IResendService resendService) : IEventHandler<SendEmailEvent>
{
    public async Task Handle(SendEmailEvent notification, CancellationToken cancellationToken)
    {
        var result = await resendService.EmailSendAsync(new Resend.EmailMessage
        {
            From = new Resend.EmailAddress
            {
                DisplayName = notification.Email.From.DisplayName,
                Email = notification.Email.From.Address
            },
            To = [.. notification.Email.To.Select(to => new Resend.EmailAddress
            {
                DisplayName = to.DisplayName,
                Email = to.Address
            })],
            Subject = notification.Email.Subject,
            HtmlBody = notification.Email.HtmlBody
        }, cancellationToken).LogExceptionAsError();
    }
}
