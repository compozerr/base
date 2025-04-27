using Api.Abstractions;
using Core.MediatR;

namespace Api.Endpoints.Projects.Project.Start;

public sealed record StartProjectCommand(
    ProjectId ProjectId) : ICommand<StartProjectResponse>;
