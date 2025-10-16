using Api.Abstractions;
using Core.MediatR;

namespace Api.Endpoints.Projects.Services.Get;

public sealed record GetProjectServicesCommand(
    ProjectId ProjectId) : ICommand<GetProjectServicesResponse>;
