using FluentValidation;

namespace Organizations.Endpoints.Invitations.InviteUser;

public sealed class InviteUserCommandValidator : AbstractValidator<InviteUserCommand>
{
    public InviteUserCommandValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email is required.")
            .EmailAddress().WithMessage("Must be a valid email address.");

        RuleFor(x => x.Role).IsInEnum();
        RuleFor(x => x.OrganizationId).NotNull();
    }
}
