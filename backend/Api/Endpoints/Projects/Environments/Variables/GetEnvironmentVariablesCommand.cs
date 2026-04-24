using Api.Abstractions;
using Api.Data;
using Core.MediatR;

namespace Api.Endpoints.Projects.Environments.Variables;

public sealed record GetEnvironmentVariablesCommand(
    ProjectId ProjectId,
    ProjectEnvironmentId EnvironmentId) : ICommand<GetProjectEnvironmentResponse?>;
