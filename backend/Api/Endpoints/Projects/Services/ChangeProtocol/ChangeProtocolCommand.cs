using Api.Abstractions;
using Api.Data;
using Core.MediatR;

namespace Api.Endpoints.Projects.Services.ChangeProtocol;

public sealed record ChangeProtocolCommand(
    ProjectId ProjectId,
    string ServiceName,
    Protocol Protocol) : ICommand<ChangeProtocolResponse>;
