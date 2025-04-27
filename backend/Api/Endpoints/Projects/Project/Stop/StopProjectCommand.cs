using Api.Abstractions;
using Core.MediatR;

namespace Api.Endpoints.Projects.Project.Stop;

public sealed record StopProjectCommand(
    ProjectId ProjectId) : ICommand<StopProjectResponse>;
