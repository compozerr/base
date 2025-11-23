using Mail;

namespace Api.Emails;

public sealed class ProjectHasBeenTerminatedTemplate : ReactEmailTemplate
{
    public ProjectHasBeenTerminatedTemplate() : base("Emails/project-has-been-terminated.html") { }

    public required string CompanyName { get; init; }
    public required string CustomerName { get; init; }
    public required string Reason { get; init; }
    public required string DashboardLink { get; init; }
    public required string ContactLink { get; init; }
    public required string CompanyAddress { get; init; }

}