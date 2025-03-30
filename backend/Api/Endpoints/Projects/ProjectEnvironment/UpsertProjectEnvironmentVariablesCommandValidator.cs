using Api.Abstractions;
using Api.Data.Repositories;
using FluentValidation;

namespace Api.Endpoints.Projects.ProjectEnvironment;

public sealed class UpsertProjectEnvironmentVariablesCommandValidator : AbstractValidator<UpsertProjectEnvironmentVariablesCommand>
{
    public UpsertProjectEnvironmentVariablesCommandValidator(IServiceScopeFactory scopeFactory)
    {
        var scope = scopeFactory.CreateScope();

        var projectRepository = scope.ServiceProvider.GetRequiredService<IProjectRepository>();

        RuleFor(x => x.Branch).MustAsync(async (command, branch, cancellationToken) =>
        {
            var environment = await projectRepository.GetProjectEnvironmentByBranchAsync(
                command.ProjectId,
                branch);

            return environment is not null;
        }).WithMessage("Branch does not exist");

        RuleForEach(x => x.Variables).ChildRules(v =>
        {
            v.RuleFor(x => x.Key)
                .NotEmpty()
                .WithMessage("Key cannot be empty")
                .MaximumLength(255)
                .WithMessage("Key cannot be longer than 100 characters");

            v.RuleFor(x => x.Value)
                .NotEmpty()
                .WithMessage("Value cannot be empty")
                .MaximumLength(20 * 1024)
                .WithMessage("Value cannot be longer than 20KB");
        });
    }
}
