using Mail;

namespace Api.Emails;

public sealed class InvoicePaymentSucceededTemplate : ReactEmailTemplate
{
    public InvoicePaymentSucceededTemplate() : base("Emails/invoice-payment-succeeded.html") { }

    public required string CompanyName { get; init; }
    public required string CustomerName { get; init; }
    public required string Currency { get; init; }
    public required string AmountPaid { get; init; }
    public required string PaymentDate { get; init; }
    public required string InvoiceLink { get; init; }
    public required string DashboardLink { get; init; }
    public required string ContactLink { get; init; }
    public required string CompanyAddress { get; init; }

}