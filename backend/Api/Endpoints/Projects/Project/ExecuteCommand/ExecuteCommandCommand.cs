using Api.Abstractions;
using Core.MediatR;

namespace Api.Endpoints.Projects.Project.ExecuteCommand;

public sealed record ExecuteCommandCommand(
    ProjectId ProjectId,
    string Command,
    string? EnvironmentName = null) : ICommand<ExecuteCommandResponse>;
