using Api.Abstractions;
using Core.MediatR;

namespace Api.Endpoints.Projects.Services.ExposeTcp;

public sealed record ExposeTcpCommand(
    ProjectId ProjectId,
    string ServiceName) : ICommand<ExposeTcpResponse>;
