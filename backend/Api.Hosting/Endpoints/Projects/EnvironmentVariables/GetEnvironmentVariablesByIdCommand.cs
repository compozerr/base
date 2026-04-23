using Api.Abstractions;
using Api.Data;
using Core.MediatR;

namespace Api.Hosting.Endpoints.Projects.EnvironmentVariables;

public sealed record GetEnvironmentVariablesByIdCommand(
    ProjectId ProjectId,
    ProjectEnvironmentId EnvironmentId) : ICommand<GetProjectEnvironmentResponse?>;
