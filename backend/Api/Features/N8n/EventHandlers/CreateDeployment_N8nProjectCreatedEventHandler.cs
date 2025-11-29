using Api.Abstractions;
using Api.Data.Events;
using Core.Abstractions;
using Github.Services;
using MediatR;

namespace Api.Features.N8n.EventHandlers;

public sealed class CreateDeployment_N8nProjectCreatedEventHandler(
    ISender sender,
    IGithubService githubService)
    : EntityDomainEventHandlerBase<N8nProjectCreatedEvent>
{
    protected override async Task HandleAfterSaveAsync(
        N8nProjectCreatedEvent domainEvent,
        CancellationToken cancellationToken)
    {
        var latestCommit = await githubService.GetLatestCommitAsync(
            domainEvent.Entity.RepoUri,
            cancellationToken);

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
