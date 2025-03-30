using Database.Events;

namespace Api.Data.Events;

public sealed record DomainChangeEvent(
    Domain Entity) : IEntityDomainEvent<Domain>;