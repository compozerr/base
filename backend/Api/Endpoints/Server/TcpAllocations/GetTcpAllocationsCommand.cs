using Api.Abstractions;
using Core.MediatR;

namespace Api.Endpoints.Server.TcpAllocations;

public sealed record GetTcpAllocationsCommand(
    ServerId ServerId) : ICommand<GetTcpAllocationsResponse>;
