using Api.Data;
using Api.Data.Repositories;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;

namespace Api.Hosting.Endpoints.Deployments.ChangeDeploymentStatus;

public sealed class ChangeDeploymentStatusCommandValidator : AbstractValidator<ChangeDeploymentStatusCommand>
{
    public ChangeDeploymentStatusCommandValidator(IServiceScopeFactory scopeFactory)
    {
        var scope = scopeFactory.CreateScope();

        RuleFor(x => x.DeploymentId)
            .NotEmpty()
            .MustAsync(async (deploymentId, cancellationToken) => await scope.ServiceProvider.GetRequiredService<IDeploymentRepository>().GetByIdAsync(deploymentId, cancellationToken) is not null)
            .WithMessage("Invalid deployment id");
    }
}
