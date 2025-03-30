using Api.Abstractions;
using Core.MediatR;

namespace Api.Endpoints.Projects.Domains.Add;

public sealed record AddDomainCommand(
    ProjectId ProjectId,
    string Domain,
    string ServiceName) : ICommand<AddDomainResponse>;