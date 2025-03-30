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
    }
}
