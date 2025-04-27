using Core.MediatR;

namespace Api.Data;

public sealed record GetProjectEnvironmentCommand(
    ProjectId ProjectId,
    string Branch) : ICommand<GetProjectEnvironmentResponse>;
