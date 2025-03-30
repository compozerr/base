using Api.Abstractions;
using Core.MediatR;

namespace Api.Endpoints.Projects.Domains.Get;

public sealed record GetDomainsCommand(
    ProjectId ProjectId) : ICommand<GetDomainsResponse>;
