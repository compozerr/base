using Api.Abstractions;
using Api.Endpoints.Projects.Services.Get;
using Core.MediatR;

namespace Api.Endpoints.Projects.Environments.Services;

public sealed record GetEnvironmentServicesCommand(
    ProjectId ProjectId,
    ProjectEnvironmentId EnvironmentId) : ICommand<GetProjectServicesResponse>;
