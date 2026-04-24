using Carter;
using Mail.Endpoints.Admin.GetSentEmail;
using Mail.Endpoints.Admin.ListSentEmails;
using Microsoft.AspNetCore.Routing;

namespace Mail.Endpoints.Admin;

public class AdminSentEmailsGroup : CarterModule
{
    public AdminSentEmailsGroup() : base("admin/sent-emails")
    {
        RequireAuthorization("admin");
        WithTags("AdminSentEmails");
    }

    public override void AddRoutes(IEndpointRouteBuilder app)
    {
        app.AddListSentEmailsRoute();
        app.AddGetSentEmailRoute();
    }
}
