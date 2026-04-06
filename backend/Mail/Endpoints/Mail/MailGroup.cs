using Carter;
using Mail.Endpoints.Mail.SendMail;
using Microsoft.AspNetCore.Routing;

namespace Mail.Endpoints.Mail;

public class MailGroup : CarterModule
{
    public MailGroup() : base("/mail")
    {
        WithTags(nameof(Mail));
    }

    public override void AddRoutes(IEndpointRouteBuilder app)
    {
        app.AddSendMailRoute();
    }
}