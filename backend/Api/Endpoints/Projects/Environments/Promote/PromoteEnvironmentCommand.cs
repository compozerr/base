using Api.Abstractions;
using Core.MediatR;

namespace Api.Endpoints.Projects.Environments.Promote;

public sealed record PromoteEnvironmentCommand(
    ProjectId ProjectId,
    ProjectEnvironmentId EnvironmentId) : ICommand<PromoteEnvironmentResponse>;
