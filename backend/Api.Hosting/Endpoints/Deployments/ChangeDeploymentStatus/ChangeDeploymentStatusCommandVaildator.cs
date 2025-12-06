using Api.Data;
using Api.Data.Repositories;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;

namespace Api.Hosting.Endpoints.Deployments.ChangeDeploymentStatus;

public sealed class ChangeDeploymentStatusCommandValidator : AbstractValidator<ChangeDeploymentStatusCommand>
{
    public ChangeDeploymentStatusCommandValidator(IServiceScopeFactory scopeFactory)
    {
        RuleFor(x => x.DeploymentId)
            .NotEmpty()
            .MustAsync(async (deploymentId, cancellationToken) =>
            {
                await using var scope = scopeFactory.CreateAsyncScope();
                var repository = scope.ServiceProvider.GetRequiredService<IDeploymentRepository>();
                return await repository.GetByIdAsync(deploymentId, cancellationToken) is not null;
            })
            .WithMessage("Invalid deployment id");
    }
}
