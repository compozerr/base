using Api.Abstractions;
using Core.MediatR;

namespace Api.Endpoints.Projects.Services.Upsert;

public sealed record UpsertProjectServicesCommand(
    ProjectId ProjectId,
    List<ServiceInfo> Services) : ICommand<UpsertProjectServicesResponse>;
