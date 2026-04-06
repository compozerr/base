using Api.Abstractions;
using Core.MediatR;

namespace Api.Endpoints.Projects.Domains.ChangeEnvironment;

public sealed record ChangeDomainEnvironmentCommand(
    ProjectId ProjectId, DomainId DomainId, ProjectEnvironmentId? EnvironmentId) : ICommand<ChangeDomainEnvironmentResponse>;

public sealed record ChangeDomainEnvironmentResponse;
