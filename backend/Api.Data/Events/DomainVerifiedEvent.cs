using Database.Events;

namespace Api.Data.Events;

public sealed record DomainVerifiedEvent(
    Domain Entity) : IEntityDomainEvent<Domain>;
