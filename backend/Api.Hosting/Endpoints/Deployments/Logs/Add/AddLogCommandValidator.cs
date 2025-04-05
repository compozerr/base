using Api.Abstractions;
using Api.Data.Repositories;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using Serilog;

namespace Api.Hosting.Endpoints.Deployments.Logs.Add;

public sealed class AddLogCommandValidator : AbstractValidator<AddLogCommand>
{
    public AddLogCommandValidator(IServiceScopeFactory scopeFactory)
    {
        var scope = scopeFactory.CreateScope();
        var deploymentRepository = scope.ServiceProvider.GetRequiredService<IDeploymentRepository>();

        RuleFor(x => x.DeploymentId).MustAsync(async (deploymentId, cancellationToken) =>
        {
            var deployment = await deploymentRepository.GetByIdAsync(
                deploymentId,
                cancellationToken);
                
            Log.ForContext("deployment", deployment).Information("Deployment");

            return deployment is { Status: Data.DeploymentStatus.Deploying };
        }).WithMessage("Deployment not found");

        RuleFor(x => x.Log).NotEmpty().WithMessage("Log is required");
    }
}
