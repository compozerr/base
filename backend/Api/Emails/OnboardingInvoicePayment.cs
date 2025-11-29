using Mail;

namespace Api.Emails;

public sealed class OnboardingInvoicePaymentTemplate : ReactEmailTemplate
{
    public OnboardingInvoicePaymentTemplate() : base("Emails/onboarding-invoice-payment.html") { }

    public required string CustomerName { get; init; }
    public required string DaysUntilTermination { get; init; }
    public required string FrontendUrl { get; init; }

}