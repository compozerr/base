using FluentValidation;

namespace Mail.Endpoints.Admin.GetSentEmail;

public sealed class GetSentEmailCommandValidator : AbstractValidator<GetSentEmailCommand>
{
    public GetSentEmailCommandValidator()
    {
        RuleFor(x => x.Id).NotNull();
    }
}
