
using Api.Abstractions;
using Api.Data.Repositories;
using Core.MediatR;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Api.Endpoints.Projects.Deployments.RedeployLatestDeployment;

public sealed class RedeployLatestDeploymentCommandHandler(
	IDeploymentRepository deploymentRepository,
	ISender sender) : ICommandHandler<RedeployLatestDeploymentCommand, RedeployLatestDeploymentResponse>
{
	public async Task<RedeployLatestDeploymentResponse> Handle(
	    RedeployLatestDeploymentCommand command,
	    CancellationToken cancellationToken = default)
	{
		var latestDeploymentForProject = await deploymentRepository.Query()
			.Where(d => d.ProjectId == command.ProjectId)
			.OrderByDescending(d => d.CreatedAtUtc)
			.FirstOrDefaultAsync(cancellationToken) ?? throw new InvalidOperationException($"No deployments found for project with ID {command.ProjectId}.");
			
        var deployProjectCommand = new DeployProjectCommand(
				command.ProjectId,
				latestDeploymentForProject.CommitHash,
				latestDeploymentForProject.CommitMessage,
				latestDeploymentForProject.CommitAuthor,
				latestDeploymentForProject.CommitBranch,
				latestDeploymentForProject.CommitEmail,
				OverrideAuthorization: true);

		await sender.Send(
			deployProjectCommand,
			cancellationToken);

		return new();
	}
}
