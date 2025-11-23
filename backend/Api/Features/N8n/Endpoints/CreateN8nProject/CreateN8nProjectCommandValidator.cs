using Api.Abstractions;
using Api.Data.Repositories;
using Auth.Services;
using FluentValidation;
using Stripe.Extensions;

namespace Api.Features.N8n.Endpoints.CreateN8nProject;

public sealed class CreateN8nProjectCommandValidator : AbstractValidator<CreateN8nProjectCommand>
{
    public CreateN8nProjectCommandValidator(
        IServiceScopeFactory scopeFactory)
    {
        var scope = scopeFactory.CreateScope();
        var projectRepository = scope.ServiceProvider.GetRequiredService<IProjectRepository>();
        var currentUserAccessor = scope.ServiceProvider.GetRequiredService<ICurrentUserAccessor>();

        RuleFor(x => x.Tier).Must(tier =>
        {
            return ServerTiers.All.Select(x => x.Id.Value).Contains(tier);
        }).WithMessage("Invalid tier specified.")
          .NotEmpty().WithMessage("Tier cannot be empty.")
          .NotNull().WithMessage("Tier cannot be null.");

        RuleFor(x => x)
            .UserMustHavePaymentMethod(scopeFactory)
            .WhenAsync(async (x, cancellationToken) =>
        {
            var userId = currentUserAccessor.CurrentUserId;

            var n8nProjectsCount = await projectRepository.CountAsync(
                x => x.UserId == userId && x.Type == ProjectType.N8n,
                cancellationToken);

            return n8nProjectsCount >= 1;
        }).WithMessage("A payment method is required to create more than one n8n project.");
    }
}