using Core.MediatR;

namespace Api.Data;

public sealed record GetProjectEnvironmentCommand(
    Guid ProjectId,
    string Branch) : ICommand<GetProjectEnvironmentResponse>;
