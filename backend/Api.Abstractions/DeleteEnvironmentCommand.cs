using Core.MediatR;

namespace Api.Abstractions;

public sealed record DeleteEnvironmentCommand(
    ProjectId ProjectId,
    ProjectEnvironmentId EnvironmentId) : ICommand<DeleteEnvironmentResponse>;

public sealed record DeleteEnvironmentResponse;
