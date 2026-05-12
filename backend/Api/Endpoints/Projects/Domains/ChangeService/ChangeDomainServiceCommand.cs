using Api.Abstractions;
using Api.Data;
using Core.MediatR;

namespace Api.Endpoints.Projects.Domains.ChangeService;

public sealed record ChangeDomainServiceCommand(
    ProjectId ProjectId,
    DomainId DomainId,
    string ServiceName,
    Protocol Protocol) : ICommand<ChangeDomainServiceResponse>;

public sealed record ChangeDomainServiceResponse;
