using Api.Abstractions;
using Api.Data;
using Api.Data.Repositories;
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

    private CreateEnvironmentCommandHandler NewHandler() => new(
        _environmentRepository,
        _projectRepository,
        _serverRepository,
        _serverAllocator,
        _subscriptionsService);

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
}
