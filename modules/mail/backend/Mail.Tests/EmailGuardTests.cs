using FluentAssertions;
using MailResend;

namespace Mail.Tests;

public class EmailGuardTests
{
    [Fact]
    public void ThrowsWhenSubjectContainsOpeningBraces()
    {
        var act = () => EmailGuard.ThrowIfUnresolvedPlaceholders("Hello {{name}}", "<p>Clean body</p>");

        act.Should().Throw<UnresolvedPlaceholderException>()
           .WithMessage("*subject*");
    }

    [Fact]
    public void ThrowsWhenSubjectContainsClosingBraces()
    {
        var act = () => EmailGuard.ThrowIfUnresolvedPlaceholders("Hello name}}", "<p>Clean body</p>");

        act.Should().Throw<UnresolvedPlaceholderException>()
           .WithMessage("*subject*");
    }

    [Fact]
    public void ThrowsWhenBodyContainsOpeningBraces()
    {
        var act = () => EmailGuard.ThrowIfUnresolvedPlaceholders("Clean subject", "<p>Hello {{name}}</p>");

        act.Should().Throw<UnresolvedPlaceholderException>()
           .WithMessage("*body*");
    }

    [Fact]
    public void ThrowsWhenBodyContainsClosingBraces()
    {
        var act = () => EmailGuard.ThrowIfUnresolvedPlaceholders("Clean subject", "<p>Hello name}}</p>");

        act.Should().Throw<UnresolvedPlaceholderException>()
           .WithMessage("*body*");
    }

    [Fact]
    public void ThrowsWhenBothSubjectAndBodyContainBraces()
    {
        var act = () => EmailGuard.ThrowIfUnresolvedPlaceholders("{{subject}}", "<p>{{body}}</p>");

        act.Should().Throw<UnresolvedPlaceholderException>()
           .WithMessage("*subject and body*");
    }

    [Fact]
    public void DoesNotThrowForCleanContent()
    {
        var act = () => EmailGuard.ThrowIfUnresolvedPlaceholders(
            "Welcome to Compozerr",
            "<p>Hello John, your account is ready.</p>");

        act.Should().NotThrow();
    }

    [Fact]
    public void DoesNotThrowForSingleBraces()
    {
        var act = () => EmailGuard.ThrowIfUnresolvedPlaceholders(
            "Welcome {user}",
            "<style>body { color: red; }</style>");

        act.Should().NotThrow();
    }

    [Fact]
    public void ThrowsCorrectExceptionType()
    {
        var act = () => EmailGuard.ThrowIfUnresolvedPlaceholders("{{x}}", "clean");

        act.Should().Throw<UnresolvedPlaceholderException>();
    }
}
