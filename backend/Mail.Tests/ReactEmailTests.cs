using Core.Services;
using FluentAssertions;
using Mail.Services;
using Moq;

namespace Mail.Tests;

public class ReactEmailTests
{
    private readonly Mock<IFrontendLocation> frontendLocation = new();

    public ReactEmailTests()
    {
        frontendLocation.Setup(l => l.GetFromPath(It.IsAny<string>()))
            .Returns((string path) => new Uri("https://example.com" + path));
    }

    [Fact]
    public async Task CreateAsync_ThrowsIfNotEverythingReplaced()
    {
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => ReactEmail.CreateAsync(
            new EmailAddress("hosting-noreply@notifications.compozerr.com", "compozerr hosting"),
            [new EmailAddress("email@example.com", "User Name")],
            "Bla bla",
            new MissingPaymentMethodTemplate()
            {
                CustomerName = "User Name",
                CompanyName = "Compozerr Hosting",
                Description = "Some description",
                // AddPaymentMethodUrl MISSING
                DashboardLink = frontendLocation.Object.GetFromPath("/projects").ToString(),
                ContactLink = frontendLocation.Object.GetFromPath("/contact").ToString(),
                CompanyAddress = "Whatever"
            }));

        exception.Message.Should().Contain("Unmatched placeholder found in template:");
    }

    [Fact]
    public async Task CreateAsync_ReplacesEventhoughItHasNewLine()
    {
        var mail = await ReactEmail.CreateAsync(
            new EmailAddress("hosting-noreply@notifications.compozerr.com", "compozerr hosting"),
            [new EmailAddress("email@example.com", "User Name")],
            "Bla bla",
            new OnboardingInvoicePaymentTemplate()
            {
                CustomerName = "User Name",
                DaysUntilTermination = "5",
                DashboardUrl = frontendLocation.Object.GetFromPath("/projects").ToString(),
                AddPaymentMethodUrl = frontendLocation.Object.GetFromPath("/add-payment-method").ToString(),
                ContactLink = frontendLocation.Object.GetFromPath("/contact").ToString(),
                CompanyAddress = "Whatever"
            });

        mail.Should().NotBeNull();
        mail.HtmlBody.Should().NotMatchRegex("%\\s*\\w+\\s*%");
    }
}

public sealed class MissingPaymentMethodTemplate : ReactEmailTemplate
{
    public MissingPaymentMethodTemplate() : base("Emails/missing-payment-method-mock.html") { }

    public required string CompanyName { get; init; }
    public required string CustomerName { get; init; }
    public required string Description { get; init; }
    public required string DashboardLink { get; init; }
    public required string ContactLink { get; init; }
    public required string CompanyAddress { get; init; }

}

public sealed class OnboardingInvoicePaymentTemplate : ReactEmailTemplate
{
    public OnboardingInvoicePaymentTemplate() : base("Emails/onboarding-invoice-payment-mock.html") { }

    public required string CustomerName { get; init; }
    public required string DaysUntilTermination { get; init; }
    public required string AddPaymentMethodUrl { get; init; }
    public required string DashboardUrl { get; init; }
    public required string ContactLink { get; init; }
    public required string CompanyAddress { get; init; }

}