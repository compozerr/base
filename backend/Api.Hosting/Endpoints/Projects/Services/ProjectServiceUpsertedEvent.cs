using Api.Data;
using Database.Events;

namespace Api.Hosting.Endpoints.Projects.Services;

public sealed record ProjectServiceUpsertedEvent(
    ProjectService Entity) : IEntityDomainEvent<ProjectService>, IDispatchBeforeSaveChanges;