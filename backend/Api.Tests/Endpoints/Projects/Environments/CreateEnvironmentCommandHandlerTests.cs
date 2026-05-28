using Api.Abstractions;
using Api.Abstractions.Exceptions;
using Api.Data;
using Api.Data.Repositories;
using Api.Data.Services;
using Api.Endpoints.Projects.Environments.Create;
using Auth.Abstractions;
using Cli.Endpoints.Projects;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Stripe.Endpoints.Subscriptions.GetUserSubscriptions;
using Stripe.Services;

namespace Api.Tests.Endpoints.Projects.Environments;

/// <summary>
/// Regression tests for <see cref="CreateEnvironmentCommandHandler"/>, which had no
/// coverage before Spec B added server allocation to the env-creation path (R5).
///
/// Covers:
///   - explicit <see cref="CreateEnvironmentCommand.ServerId"/> is honored.
///   - allocator fallback runs when no ServerId is supplied.
///   - if env-creation throws after subscription creation, the just-created
///     Stripe subscription is compensated (cancelled).
/// </summary>
public class CreateEnvironmentCommandHandlerTests
{
    private readonly IProjectEnvironmentRepository _environmentRepository = Substitute.For<IProjectEnvironmentRepository>();
    private readonly IProjectRepository _projectRepository = Substitute.For<IProjectRepository>();
    private readonly IServerRepository _serverRepository = Substitute.For<IServerRepository>();
    private readonly IServerAllocator _serverAllocator = Substitute.For<IServerAllocator>();
    private readonly ISubscriptionsService _subscriptionsService = Substitute.For<ISubscriptionsService>();
    private readonly IServerVisibilityFilter _serverVisibilityFilter = Substitute.For<IServerVisibilityFilter>();

    public CreateEnvironmentCommandHandlerTests()
    {
        // Default: every candidate server is visible to the caller. Tests that
        // exercise the AdminOnly-bypass rejection override this for their server.
        _serverVisibilityFilter
            .ApplyForCurrentUser(Arg.Any<IEnumerable<Server>>())
            .Returns(ci => ci.Arg<IEnumerable<Server>>());
    }

    private CreateEnvironmentCommandHandler NewHandler() => new(
        _environmentRepository,
        _projectRepository,
        _serverRepository,
        _serverAllocator,
        _subscriptionsService,
        _serverVisibilityFilter);

    private static Project NewProject(ProjectId id, LocationId? locationId = null) => new()
    {
        Id = id,
        Name = "test",
        RepoUri = new Uri("https://github.com/test/repo"),
        UserId = UserId.CreateNew(),
        LocationId = locationId ?? LocationId.CreateNew(),
        State = ProjectState.Running,
        ServerTierId = new ServerTierId("T0"),
    };

    private static SubscriptionDto NewSubscription(ProjectId projectId) => new(
        Id: "sub_test_123",
        ProjectId: projectId,
        Name: "test",
        Status: "active",
        PlanId: "plan_test",
        ServerTierId: "T0",
        CurrentPeriodStart: DateTime.UtcNow,
        CurrentPeriodEnd: DateTime.UtcNow.AddDays(30),
        CancelAtPeriodEnd: false,
        Amount: new Money(amount: 0m, currency: "usd"));

    private void SetupSubscriptionCreate(ProjectId projectId)
    {
        _subscriptionsService.CreateSubscriptionTierAsync(
            projectId,
            Arg.Any<ServerTierId>(),
            Arg.Any<string?>(),
            Arg.Any<string?>(),
            Arg.Any<CancellationToken>())
            .Returns(NewSubscription(projectId));
    }

    [Fact]
    public async Task ExplicitServerId_IsHonored_AndAllocatorIsNotCalled()
    {
        var projectId = ProjectId.CreateNew();
        var project = NewProject(projectId);
        var requestedServerId = ServerId.CreateNew();
        var server = new Server
        {
            Id = requestedServerId,
            SecretId = SecretId.CreateNew(),
            ServerVisibility = ServerVisibility.Public,
        };

        _projectRepository.GetByIdAsync(projectId, Arg.Any<CancellationToken>()).Returns(project);
        _serverRepository.GetByIdAsync(requestedServerId, Arg.Any<CancellationToken>()).Returns(server);
        SetupSubscriptionCreate(projectId);

        ProjectEnvironment? captured = null;
        await _environmentRepository.AddAsync(
            Arg.Do<ProjectEnvironment>(e => captured = e),
            Arg.Any<CancellationToken>());

        var handler = NewHandler();

        var result = await handler.Handle(new CreateEnvironmentCommand(
            ProjectId: projectId,
            Name: "preview-explicit",
            Branch: "feature/x",
            Tier: "T0",
            Type: EnvironmentType.Preview,
            ServerId: requestedServerId));

        Assert.NotNull(captured);
        Assert.Equal(requestedServerId, captured!.ServerId);
        Assert.Equal(EnvironmentType.Preview, captured.Type);
        Assert.NotNull(result);

        await _serverAllocator.DidNotReceiveWithAnyArgs().AllocateForLocationAsync(default!, default);
    }

    [Fact]
    public async Task NullServerId_FallsBackToAllocator_AndUsesProjectLocation()
    {
        var projectId = ProjectId.CreateNew();
        var locationId = LocationId.CreateNew();
        var project = NewProject(projectId, locationId);
        var allocatedServerId = ServerId.CreateNew();

        _projectRepository.GetByIdAsync(projectId, Arg.Any<CancellationToken>()).Returns(project);
        _serverAllocator.AllocateForLocationAsync(locationId, Arg.Any<CancellationToken>())
            .Returns(new ServerAllocationResult(allocatedServerId, ChangedToLocationId: null));
        SetupSubscriptionCreate(projectId);

        ProjectEnvironment? captured = null;
        await _environmentRepository.AddAsync(
            Arg.Do<ProjectEnvironment>(e => captured = e),
            Arg.Any<CancellationToken>());

        var handler = NewHandler();

        await handler.Handle(new CreateEnvironmentCommand(
            ProjectId: projectId,
            Name: "preview-auto",
            Branch: "feature/y",
            Tier: "T0",
            Type: EnvironmentType.Preview,
            ServerId: null));

        Assert.NotNull(captured);
        Assert.Equal(allocatedServerId, captured!.ServerId);

        await _serverAllocator.Received(1).AllocateForLocationAsync(locationId, Arg.Any<CancellationToken>());
        await _serverRepository.DidNotReceiveWithAnyArgs().GetByIdAsync(default!, default(CancellationToken));
    }

    [Fact]
    public async Task EnvironmentAddFailure_CancelsJustCreatedSubscription()
    {
        var projectId = ProjectId.CreateNew();
        var project = NewProject(projectId);
        var subscription = NewSubscription(projectId);

        _projectRepository.GetByIdAsync(projectId, Arg.Any<CancellationToken>()).Returns(project);
        _serverAllocator.AllocateForLocationAsync(project.LocationId, Arg.Any<CancellationToken>())
            .Returns(new ServerAllocationResult(ServerId.CreateNew(), ChangedToLocationId: null));
        _subscriptionsService.CreateSubscriptionTierAsync(
            projectId,
            Arg.Any<ServerTierId>(),
            Arg.Any<string?>(),
            Arg.Any<string?>(),
            Arg.Any<CancellationToken>())
            .Returns(subscription);

        _environmentRepository
            .AddAsync(Arg.Any<ProjectEnvironment>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new InvalidOperationException("simulated DB failure"));

        var handler = NewHandler();

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            handler.Handle(new CreateEnvironmentCommand(
                ProjectId: projectId,
                Name: "preview-fail",
                Branch: "feature/z",
                Tier: "T0",
                Type: EnvironmentType.Preview,
                ServerId: null)));

        await _subscriptionsService.Received(1).CancelSubscriptionAsync(
            subscription.Id,
            cancelImmediately: true,
            Arg.Any<CancellationToken>());
    }

    // F-P1-3: server allocation runs INSIDE the compensation scope. If the
    // allocator throws (e.g. no visible server in the location), the just-created
    // Stripe subscription must be cancelled — the prior code allocated before the
    // try/catch and leaked billing on an allocation failure.
    [Fact]
    public async Task ServerAllocationThrows_SubscriptionCompensation_CancelsSubscription()
    {
        var projectId = ProjectId.CreateNew();
        var project = NewProject(projectId);
        var subscription = NewSubscription(projectId);

        _projectRepository.GetByIdAsync(projectId, Arg.Any<CancellationToken>()).Returns(project);
        _subscriptionsService.CreateSubscriptionTierAsync(
            projectId,
            Arg.Any<ServerTierId>(),
            Arg.Any<string?>(),
            Arg.Any<string?>(),
            Arg.Any<CancellationToken>())
            .Returns(subscription);

        // Allocation (no explicit ServerId) throws — simulating "no server visible
        // in this location for your account".
        _serverAllocator.AllocateForLocationAsync(project.LocationId, Arg.Any<CancellationToken>())
            .ThrowsAsync(new NoAvailableServerException("No server available in location for your account."));

        var handler = NewHandler();

        await Assert.ThrowsAsync<NoAvailableServerException>(() =>
            handler.Handle(new CreateEnvironmentCommand(
                ProjectId: projectId,
                Name: "preview-alloc-fail",
                Branch: "feature/alloc",
                Tier: "T0",
                Type: EnvironmentType.Preview,
                ServerId: null)));

        // Billing rolled back, and the env was never persisted.
        await _subscriptionsService.Received(1).CancelSubscriptionAsync(
            subscription.Id,
            cancelImmediately: true,
            Arg.Any<CancellationToken>());
        await _environmentRepository.DidNotReceiveWithAnyArgs()
            .AddAsync(default!, default);
    }

    // F-P1-4: an explicit ServerId must be run through IServerVisibilityFilter.
    // A non-admin hand-crafting a request with an AdminOnly server's id must be
    // rejected with a controlled validation error that does NOT reveal the
    // server exists — and the just-created subscription is compensated.
    [Fact]
    public async Task ExplicitAdminOnlyServer_AdminOnlyBypassRejection_IsRejectedWithoutLeakingExistence()
    {
        var projectId = ProjectId.CreateNew();
        var project = NewProject(projectId);
        var requestedServerId = ServerId.CreateNew();
        var subscription = NewSubscription(projectId);
        var adminOnlyServer = new Server
        {
            Id = requestedServerId,
            SecretId = SecretId.CreateNew(),
            ServerVisibility = ServerVisibility.AdminOnly,
        };

        _projectRepository.GetByIdAsync(projectId, Arg.Any<CancellationToken>()).Returns(project);
        _serverRepository.GetByIdAsync(requestedServerId, Arg.Any<CancellationToken>()).Returns(adminOnlyServer);
        SetupSubscriptionCreate(projectId);

        // The filter hides the AdminOnly server from this (non-admin) caller.
        _serverVisibilityFilter
            .ApplyForCurrentUser(Arg.Any<IEnumerable<Server>>())
            .Returns(Array.Empty<Server>());

        var handler = NewHandler();

        var ex = await Assert.ThrowsAsync<NoAvailableServerException>(() =>
            handler.Handle(new CreateEnvironmentCommand(
                ProjectId: projectId,
                Name: "preview-bypass",
                Branch: "feature/bypass",
                Tier: "T0",
                Type: EnvironmentType.Preview,
                ServerId: requestedServerId)));

        // Must not leak that the server exists.
        Assert.DoesNotContain(requestedServerId.Value.ToString(), ex.Message);

        // Never persisted, and billing compensated.
        await _environmentRepository.DidNotReceiveWithAnyArgs().AddAsync(default!, default);
        await _subscriptionsService.Received(1).CancelSubscriptionAsync(
            subscription.Id,
            cancelImmediately: true,
            Arg.Any<CancellationToken>());
    }
}
