using Api.Abstractions;
using Core.MediatR;

namespace Api.Endpoints.Projects.Environments.Get;

public sealed record GetEnvironmentsCommand(
    ProjectId ProjectId) : ICommand<GetEnvironmentsResponse>;
