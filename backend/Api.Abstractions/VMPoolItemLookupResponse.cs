namespace Api.Abstractions;

public sealed record VMPoolItemLookupResponse(
    bool Found,
    VMPoolItemId? VMPoolItemId);