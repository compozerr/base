using Core.MediatR;

namespace Api.Abstractions;

public sealed record VMPoolItemLookupRequest(
    LocationId LocationId,
    ServerTierId ServerTierId,
    ProjectType ProjectType) : ICommand<VMPoolItemLookupResponse>;