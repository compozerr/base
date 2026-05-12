using Api.Data;
using Api.Data.Extensions;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace Api.Endpoints.Projects.Domains.ChangeService;

public sealed class ChangeDomainServiceCommandValidator : AbstractValidator<ChangeDomainServiceCommand>
{
    public ChangeDomainServiceCommandValidator(IServiceScopeFactory scopeFactory)
    {
        RuleFor(x => x.ServiceName)
            .NotEmpty()
            .WithMessage("ServiceName cannot be empty");

        RuleFor(x => x.DomainId).MustBeOwnedByCallerAsync(scopeFactory);

        RuleFor(x => x.ServiceName)
            .MustAsync(async (command, serviceName, cancellationToken) =>
            {
                await using var scope = scopeFactory.CreateAsyncScope();
                var context = scope.ServiceProvider.GetRequiredService<ApiDbContext>();

                return await context.ProjectServices
                    .AnyAsync(s => s.ProjectId == command.ProjectId && s.Name == serviceName, cancellationToken);
            })
            .WithMessage("Service does not exist for this project");
    }
}
