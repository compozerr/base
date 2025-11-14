using System;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;
using MediatR;
using Database.Data;
using Database.Models;
using Core.Abstractions;
using Database.Events;
using Database.Extensions;

namespace Database.Tests;

// Test entity and event for domain event dispatching

public sealed record TestEntityId : IdBase<TestEntityId>, IId<TestEntityId>
{
    public TestEntityId(Guid value) : base(value) { }
    public static TestEntityId Create(Guid value) => new(value);
    // Parameterless constructor for EF Core
    private TestEntityId() : base(Guid.Empty) { }
}

public class TestEntity : BaseEntityWithId<TestEntityId> { }

public record TestEntityDomainEvent(TestEntity Entity) : IEntityDomainEvent<TestEntity>;
public record TestDomainEvent(string SomeValue) : IDomainEvent;

public class TestDbContext(DbContextOptions options, IMediator mediator) : BaseDbContext<TestDbContext>("test", options, mediator)
{
    public DbSet<TestEntity> TestEntities => Set<TestEntity>();
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.Entity<TestEntity>().HasKey(e => e.Id);
        // Add value converter for TestEntityId to Guid
        modelBuilder.Entity<TestEntity>()
            .Property(e => e.Id)
            .HasConversion(
                v => v.Value,
                v => TestEntityId.Create(v)
            );
    }
}

public class BaseDbContextTests
{
    [Fact]
    public async Task SaveChangesAsync_DispatchesDomainEvents_BothSaves()
    {
        var mediatorMock = new Mock<IMediator>();

        var options = new DbContextOptionsBuilder<TestDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        var entity = new TestEntity();

        entity.QueueDomainEvent<TestEntityDomainEvent>();

        var publishedEvents = new List<IDomainEvent>();
        mediatorMock.Setup(m => m.Publish(It.IsAny<IDomainEvent>(), It.IsAny<CancellationToken>()))
                   .Callback<IDomainEvent, CancellationToken>((evt, _) => publishedEvents.Add(evt));

        using var ctx = new TestDbContext(options, mediatorMock.Object);

        ctx.TestEntities.Add(entity);

        await ctx.SaveChangesAsync();

        Assert.Equal(2, publishedEvents.Count);

        Assert.Single(
            publishedEvents.OfType<DomainEventEnvelope<TestEntityDomainEvent>>(),
            e => e.Timing == DomainEventTiming.BeforeSaveChanges);

        Assert.Single(
            publishedEvents.OfType<DomainEventEnvelope<TestEntityDomainEvent>>(),
            e => e.Timing == DomainEventTiming.AfterSaveChanges);
    }

    [Fact]
    public async Task SaveChangesAsync_DispatchesDomainEvents_NormalDispatch()
    {
        var mediatorMock = new Mock<IMediator>();

        var options = new DbContextOptionsBuilder<TestDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        var entity = new TestEntity();

        entity.QueueDomainEvent(new TestDomainEvent("TestValue"));

        var publishedEvents = new List<IDomainEvent>();
        mediatorMock.Setup(m => m.Publish(It.IsAny<IDomainEvent>(), It.IsAny<CancellationToken>()))
                   .Callback<IDomainEvent, CancellationToken>((evt, _) => publishedEvents.Add(evt));

        using var ctx = new TestDbContext(options, mediatorMock.Object);

        ctx.TestEntities.Add(entity);

        await ctx.SaveChangesAsync();

        Assert.Single(publishedEvents);

        Assert.Single(
            publishedEvents.OfType<DomainEventEnvelope<TestDomainEvent>>(),
            e => e.Timing == DomainEventTiming.AfterSaveChanges);
    }
}
