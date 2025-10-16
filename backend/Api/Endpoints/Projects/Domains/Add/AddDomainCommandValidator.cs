using Api.Data;
using Api.Data.Repositories;
using Auth.Services;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace Api.Endpoints.Projects.Domains.Add;

public sealed class AddDomainCommandValidator : AbstractValidator<AddDomainCommand>
{
    public AddDomainCommandValidator(IServiceScopeFactory scopeFactory)
    {
        var scope = scopeFactory.CreateScope();

        var domainRepository = scope.ServiceProvider.GetRequiredService<IDomainRepository>();
        var projectRepository = scope.ServiceProvider.GetRequiredService<IProjectRepository>();
        var currentUserAccessor = scope.ServiceProvider.GetRequiredService<ICurrentUserAccessor>();
        var context = scope.ServiceProvider.GetRequiredService<ApiDbContext>();

        RuleFor(x => x.Domain)
            .NotEmpty()
            .WithMessage("Domain cannot be empty")
            .MaximumLength(255)
            .WithMessage("Domain cannot be longer than 255 characters");

        RuleFor(x => x.Domain)
            .Matches(@"^[a-zA-Z0-9\-\.]+\.[a-zA-Z]{2,}$")
            .WithMessage("Domain is not valid");

        RuleFor(x => x.ProjectId)
          .MustAsync(async (command, projectId, cancellationToken) =>
          {
              var project = await projectRepository.GetByIdAsync(
                  projectId,
                  cancellationToken);

              return project?.UserId == currentUserAccessor.CurrentUserId;
          })
          .WithErrorCode("403");

        RuleFor(x => x.Domain)
            .MustAsync(async (command, domain, cancellationToken) =>
            {
                var isUnique = await domainRepository.IsProjectDomainUniqueAsync(
                    command.ProjectId,
                    domain);

                return isUnique;
            })
            .WithMessage("Domain already exists");

        RuleFor(x => x.ServiceName)
            .MustAsync(async (command, serviceName, cancellationToken) =>
            {
                var serviceExists = await context.ProjectServices
                    .AnyAsync(s => s.ProjectId == command.ProjectId && s.Name == serviceName, cancellationToken);

                return serviceExists;
            })
            .WithMessage("Service does not exist for this project");
    }
}
