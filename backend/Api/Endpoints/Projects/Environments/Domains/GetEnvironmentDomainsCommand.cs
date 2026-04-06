using Api.Abstractions;
using Api.Endpoints.Projects.Domains.Get;
using Core.MediatR;

namespace Api.Endpoints.Projects.Environments.Domains;

public sealed record GetEnvironmentDomainsCommand(
    ProjectId ProjectId,
    ProjectEnvironmentId EnvironmentId) : ICommand<GetDomainsResponse>;
