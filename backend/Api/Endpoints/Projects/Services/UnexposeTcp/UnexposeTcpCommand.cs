using Api.Abstractions;
using Core.MediatR;

namespace Api.Endpoints.Projects.Services.UnexposeTcp;

public sealed record UnexposeTcpCommand(
    ProjectId ProjectId,
    string ServiceName) : ICommand<UnexposeTcpResponse>;
