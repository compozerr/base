using Api.Abstractions;
using Api.Data;
using Core.MediatR;

namespace Api.Endpoints.Projects.Domains.Add;

public sealed record AddDomainCommand(
    ProjectId ProjectId,
    string Domain,
    string ServiceName,
    Protocol Protocol,
    ProjectEnvironmentId? EnvironmentId = null) : ICommand<AddDomainResponse>;