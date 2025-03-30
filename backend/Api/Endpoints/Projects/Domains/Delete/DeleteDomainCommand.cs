using Api.Abstractions;
using Core.MediatR;

namespace Api.Endpoints.Projects.Domains.Delete;

public sealed record DeleteDomainCommand(
    DomainId DomainId) : ICommand<DeleteDomainResponse>;
