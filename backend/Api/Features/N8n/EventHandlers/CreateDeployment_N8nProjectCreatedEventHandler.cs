using Api.Abstractions;
using Api.Features.N8n.Events;
using Core.Abstractions;
using Github.Services;
using MediatR;

namespace Api.Features.N8n.EventHandlers;

public sealed class CreateDeployment_N8nProjectCreatedEventHandler(
    ISender sender,
    IGithubService githubService)
    : DomainEventHandlerBase<N8nProjectCreatedEvent>
{
    public override async Task HandleAfterSaveChangesAsync(
        N8nProjectCreatedEvent domainEvent,
        CancellationToken cancellationToken)
    {
        var latestCommit = await githubService.GetLatestCommitAsync(domainEvent.Entity.RepoUri);

        await sender.Send(
            new DeployProjectCommand(
                domainEvent.Entity.Id,
                latestCommit.Sha,
                latestCommit.Commit.Message,
                latestCommit.Commit.Author.Name,
                latestCommit.Commit.Committer.Name,
                latestCommit.Commit.Author.Email),
            cancellationToken);
    }
}
