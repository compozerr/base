using Api.Abstractions;
using Core.MediatR;

namespace Api.Endpoints.Projects.Domains.Parent.Get;

public sealed record GetParentDomainCommand(
    DomainId DomainId) : ICommand<GetParentDomainResponse>;
