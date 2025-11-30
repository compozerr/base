using Mail;

namespace Api.Emails;

public sealed class ProjectTerminatedUnpaidInvoiceTemplate : ReactEmailTemplate
{
    public ProjectTerminatedUnpaidInvoiceTemplate() : base("Emails/project-terminated-unpaid-invoice.html") { }

    public required string CompanyName { get; init; }
    public required string CustomerName { get; init; }
    public required string Currency { get; init; }
    public required string AmountDue { get; init; }
    public required string DueDate { get; init; }
    public required string DaysOverdue { get; init; }
    public required string PaymentLink { get; init; }
    public required string DashboardLink { get; init; }
    public required string ContactLink { get; init; }
    public required string CompanyAddress { get; init; }

}