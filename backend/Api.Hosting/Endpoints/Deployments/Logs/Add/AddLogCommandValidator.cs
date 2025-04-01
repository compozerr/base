using Api.Abstractions;
using Api.Data.Repositories;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;

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
                DeploymentId.Create(
                    deploymentId), cancellationToken);

            return deployment != null;
        }).WithMessage("Deployment not found");

        RuleFor(x => x.Log).NotEmpty().WithMessage("Log is required");
    }
}
