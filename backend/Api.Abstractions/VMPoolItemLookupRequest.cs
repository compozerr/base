using Api.Abstractions;
using Core.MediatR;

namespace Api.Hosting.VMPooling.Core.VMPoolItemLookup;

public sealed record VMPoolItemLookupRequest(
    LocationId LocationId,
    ServerTierId ServerTierId,
    ProjectType ProjectType) : ICommand<VMPoolItemLookupResponse>;