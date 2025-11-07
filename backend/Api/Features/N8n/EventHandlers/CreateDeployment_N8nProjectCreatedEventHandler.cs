using Api.Abstractions;
using Api.Features.N8n.Events;
using Core.Abstractions;
using Github.Services;
using MediatR;

namespace Api.Features.N8n.EventHandlers;

public sealed class CreateDeployment_N8nProjectCreatedEventHandler(
    ISender sender,
    IGithubService githubService)
    : IDomainEventHandler<N8nProjectCreatedEvent>
{
    public async Task Handle(N8nProjectCreatedEvent notification, CancellationToken cancellationToken)
    {
        var latestCommit = await githubService.GetLatestCommitAsync(notification.Entity.RepoUri);

        await sender.Send(
            new DeployProjectCommand(
                notification.Entity.Id,
                latestCommit.Sha,
                latestCommit.Commit.Message,
                latestCommit.Commit.Author.Name,
                latestCommit.Commit.Committer.Name,
                latestCommit.Commit.Author.Email),
            cancellationToken);
    }
}
