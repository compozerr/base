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
        RuleFor(x => x.DeploymentId).MustAsync(async (deploymentId, cancellationToken) =>
        {
            await using var scope = scopeFactory.CreateAsyncScope();
            var deploymentRepository = scope.ServiceProvider.GetRequiredService<IDeploymentRepository>();

            var deployment = await deploymentRepository.GetByIdAsync(
                deploymentId,
                cancellationToken);

            Log.ForContext("deployment", deployment, true)
               .Information("Deployment");

            return deployment is { };
        }).WithMessage("Deployment not found");

        RuleFor(x => x.Log).NotEmpty().WithMessage("Log is required");
    }
}
