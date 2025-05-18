using Api.Abstractions;
using Core.MediatR;

namespace Cli.Endpoints.Projects.RestoreProject;

public sealed record RestoreProjectCommand(
    ProjectId ProjectId) : ICommand<RestoreProjectResponse>;
