using Api.Abstractions;
using Api.Data;
using Api.Data.Events;
using Api.Hosting.EventHandlers;
using Api.Hosting.Services;
using Api.Hosting.Services.EnvironmentTargetResolver;
using Core.Abstractions;
using NSubstitute;

namespace Api.Tests.Endpoints.Projects.ProjectDomains;

/// <summary>
/// Spec B: a project's envs may live on different servers. A domain-change
/// event must trigger an UpdateDomainsForProject push on every server that
/// hosts an env of the project, otherwise Traefik on the non-prod server stays
/// stale and the preview env stops resolving.
/// </summary>
public class UpdateDomains_FanOutTests
{
    private static InternalDomain MakeDomain(ProjectId projectId)
        => new()
        {
            Id = DomainId.CreateNew(),
            ProjectId = projectId,
            ServiceName = "Web",
            Port = "3000",
            Protocol = Protocol.Http,
            Subdomain = "test",
        };

    [Fact]
    public async Task HandleAfterSave_PushesToEveryDistinctEnvServer()
    {
        var projectId = ProjectId.CreateNew();
        var apiA = Substitute.For<IHostingApi>();
        var apiB = Substitute.For<IHostingApi>();

        var resolver = Substitute.For<IEnvironmentTargetResolver>();
        resolver.GetDistinctHostingApisForProjectAsync(projectId, Arg.Any<CancellationToken>())
            .Returns(new List<IHostingApi> { apiA, apiB });

        var handler = new UpdateDomains_DomainChangeEventHandler(resolver);
        var envelope = new DomainEventEnvelope<DomainChangeEvent>(
            new DomainChangeEvent(MakeDomain(projectId)),
            DomainEventTiming.AfterSaveChanges);

        await handler.Handle(envelope, CancellationToken.None);

        await apiA.Received(1).UpdateDomainsForProjectAsync(projectId);
        await apiB.Received(1).UpdateDomainsForProjectAsync(projectId);
    }

    [Fact]
    public async Task HandleAfterSave_DoesNothing_WhenProjectHasNoServers()
    {
        // Pre-allocation race: the domain-change event fires before any env
        // has been allocated to a server. The handler must silently no-op
        // rather than throw — the prior implementation threw an
        // ArgumentException, which broke the entity save path.
        var projectId = ProjectId.CreateNew();
        var resolver = Substitute.For<IEnvironmentTargetResolver>();
        resolver.GetDistinctHostingApisForProjectAsync(projectId, Arg.Any<CancellationToken>())
            .Returns(Array.Empty<IHostingApi>());

        var handler = new UpdateDomains_DomainChangeEventHandler(resolver);
        var envelope = new DomainEventEnvelope<DomainChangeEvent>(
            new DomainChangeEvent(MakeDomain(projectId)),
            DomainEventTiming.AfterSaveChanges);

        await handler.Handle(envelope, CancellationToken.None);
        // Reaching here without throwing is the assertion.
    }
}
