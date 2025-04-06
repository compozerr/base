using Api.Abstractions;
using Core.MediatR;

namespace Api.Endpoints.Projects.Domains.SetPrimary;

public sealed record SetPrimaryCommand(
    DomainId DomainId) : ICommand<SetPrimaryResponse>;
