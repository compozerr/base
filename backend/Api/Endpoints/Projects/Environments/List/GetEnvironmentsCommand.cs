using Api.Abstractions;
using Core.MediatR;

namespace Api.Endpoints.Projects.Environments.List;

public sealed record GetEnvironmentsCommand(
    ProjectId ProjectId) : ICommand<GetEnvironmentsResponse>;
