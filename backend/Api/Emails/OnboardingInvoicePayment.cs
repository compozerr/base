using Mail;

namespace Api.Emails;

public sealed class OnboardingInvoicePaymentTemplate : ReactEmailTemplate
{
    public OnboardingInvoicePaymentTemplate() : base("Emails/onboarding-invoice-payment.html") { }

    public required string CustomerName { get; init; }
    public required string DaysUntilTermination { get; init; }
    public required string AddPaymentMethodUrl { get; init; }
    public required string DashboardUrl { get; init; }
    public required string ContactLink { get; init; }
    public required string CompanyAddress { get; init; }

}