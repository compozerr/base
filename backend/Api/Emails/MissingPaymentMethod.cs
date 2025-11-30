using Mail;

namespace Api.Emails;

public sealed class MissingPaymentMethodTemplate : ReactEmailTemplate
{
    public MissingPaymentMethodTemplate() : base("Emails/missing-payment-method.html") { }

    public required string CompanyName { get; init; }
    public required string CustomerName { get; init; }
    public required string Description { get; init; }
    public required string AddPaymentMethodUrl { get; init; }
    public required string DashboardLink { get; init; }
    public required string ContactLink { get; init; }
    public required string CompanyAddress { get; init; }

}