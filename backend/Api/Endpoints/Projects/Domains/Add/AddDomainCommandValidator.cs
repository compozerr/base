using Api.Data.Repositories;
using FluentValidation;

namespace Api.Endpoints.Projects.Domains.Add;

public sealed class AddDomainCommandValidator : AbstractValidator<AddDomainCommand>
{
    public AddDomainCommandValidator(IServiceScopeFactory scopeFactory)
    {
        var scope = scopeFactory.CreateScope();

        var domainRepository = scope.ServiceProvider.GetRequiredService<IDomainRepository>();

        RuleFor(x => x.Domain)
            .NotEmpty()
            .WithMessage("Domain cannot be empty")
            .MaximumLength(255)
            .WithMessage("Domain cannot be longer than 255 characters");

        RuleFor(x => x.Domain)
            .Matches(@"^[a-zA-Z0-9\-\.]+\.[a-zA-Z]{2,}$")
            .WithMessage("Domain is not valid");

        RuleFor(x => x.Domain)
            .MustAsync(async (command, domain, cancellationToken) =>
            {
                var isUnique = await domainRepository.IsProjectDomainUniqueAsync(
                    command.ProjectId,
                    domain);

                return isUnique;
            })
            .WithMessage("Domain already exists");

        //TODO: This should be its own entity
        RuleFor(x => x.ServiceName)
            .Must(x => x == "Frontend" || x == "Backend")
            .WithMessage("Service name must be either 'Frontend' or 'Backend'");
    }
}
