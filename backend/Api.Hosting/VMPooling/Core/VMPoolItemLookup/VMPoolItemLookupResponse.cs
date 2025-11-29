using Api.Abstractions;

namespace Api.Hosting.VMPooling.Core.VMPoolItemLookup;

public sealed record VMPoolItemLookupResponse(
    bool Found,
    VMPoolItemId? VMPoolItemId);