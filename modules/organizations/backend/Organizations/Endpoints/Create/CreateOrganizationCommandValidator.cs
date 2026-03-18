using FluentValidation;
using Organizations.Data.Repositories;

namespace Organizations.Endpoints.Create;

public sealed class CreateOrganizationCommandValidator : AbstractValidator<CreateOrganizationCommand>
{
    public CreateOrganizationCommandValidator(IOrganizationRepository organizationRepository)
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Name is required.")
            .MaximumLength(100);

        RuleFor(x => x.Slug)
            .NotEmpty().WithMessage("Slug is required.")
            .MaximumLength(100)
            .Matches("^[a-z0-9-]+$").WithMessage("Slug can only contain lowercase letters, numbers, and hyphens.")
            .MustAsync(async (slug, ct) =>
            {
                var existing = await organizationRepository.GetBySlugAsync(slug.ToLowerInvariant(), ct);
                return existing is null;
            }).WithMessage("An organization with this slug already exists.");
    }
}
