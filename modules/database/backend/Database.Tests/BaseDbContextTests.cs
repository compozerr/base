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

public record TestDomainEventBeforeSaveChanges(TestEntity Entity) : IEntityDomainEvent<TestEntity>, IDispatchBeforeSaveChanges;
public record TestDomainEventAfterSaveChanges(TestEntity Entity) : IEntityDomainEvent<TestEntity>;

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
    public async Task SaveChangesAsync_DispatchesDomainEvents_BeforeSaveChanges()
    {
        var mediatorMock = new Mock<IMediator>();

        var options = new DbContextOptionsBuilder<TestDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        var entity = new TestEntity();

        entity.QueueDomainEvent<TestDomainEventBeforeSaveChanges>();

        using var ctx = new TestDbContext(options, mediatorMock.Object);

        ctx.TestEntities.Add(entity);

        await ctx.SaveChangesAsync();

        mediatorMock.Verify(m => m.Publish(It.Is<IDomainEvent>(e => e is TestDomainEventBeforeSaveChanges), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task SaveChangesAsync_DispatchesDomainEvents_BeforeAndAfterSave()
    {
        var mediatorMock = new Mock<IMediator>();

        var options = new DbContextOptionsBuilder<TestDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        var entity = new TestEntity();

        entity.QueueDomainEvent<TestDomainEventBeforeSaveChanges>();
        entity.QueueDomainEvent<TestDomainEventAfterSaveChanges>();
        entity.QueueDomainEvent<TestDomainEventAfterSaveChanges>();

        using var ctx = new TestDbContext(options, mediatorMock.Object);

        ctx.TestEntities.Add(entity);

        await ctx.SaveChangesAsync();

        mediatorMock.Verify(m => m.Publish(It.Is<IDomainEvent>(e => e is TestDomainEventBeforeSaveChanges), It.IsAny<CancellationToken>()), Times.Once);
        mediatorMock.Verify(m => m.Publish(It.Is<IDomainEvent>(e => e is TestDomainEventAfterSaveChanges), It.IsAny<CancellationToken>()), Times.Exactly(2));
    }
}
