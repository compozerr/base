using Api.Abstractions;
using Core.MediatR;

namespace Api.Endpoints.Projects.Environments.Delete;

public sealed record DeleteEnvironmentCommand(
    EnvironmentId EnvironmentId) : ICommand<DeleteEnvironmentResponse>;
