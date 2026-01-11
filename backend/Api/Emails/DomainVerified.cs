using Mail;

namespace Api.Emails;

public sealed class DomainVerifiedTemplate : ReactEmailTemplate
{
    public DomainVerifiedTemplate() : base("Emails/domain-verified.html") { }

    public required string CompanyName { get; init; }
    public required string CustomerName { get; init; }
    public required string DomainName { get; init; }
    public required string ProjectName { get; init; }
    public required string DashboardLink { get; init; }
    public required string DomainLink { get; init; }
    public required string CompanyAddress { get; init; }

}