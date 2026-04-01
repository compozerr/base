using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using Organizations.Data.Repositories;

namespace Organizations.Endpoints.Create;

public sealed class CreateOrganizationCommandValidator : AbstractValidator<CreateOrganizationCommand>
{
    public CreateOrganizationCommandValidator(IServiceScopeFactory scopeFactory)
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
                await using var scope = scopeFactory.CreateAsyncScope();
                var organizationRepository = scope.ServiceProvider.GetRequiredService<IOrganizationRepository>();
                var existing = await organizationRepository.GetBySlugAsync(slug.ToLowerInvariant(), ct);
                return existing is null;
            }).WithMessage("An organization with this slug already exists.");
    }
}
