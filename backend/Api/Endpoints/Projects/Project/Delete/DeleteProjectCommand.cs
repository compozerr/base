using Api.Abstractions;
using Core.MediatR;

namespace Api.Endpoints.Projects.Project.Delete;

public sealed record DeleteProjectCommand(
    ProjectId ProjectId) : ICommand<DeleteProjectResponse>;
