using FluentValidation;

namespace Mail.Endpoints.Admin.ListSentEmails;

public sealed class ListSentEmailsCommandValidator : AbstractValidator<ListSentEmailsCommand>
{
    public ListSentEmailsCommandValidator()
    {
        RuleFor(x => x.Limit)
            .InclusiveBetween(1, 200);
    }
}
