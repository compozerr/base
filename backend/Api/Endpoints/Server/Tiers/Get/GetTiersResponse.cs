using Api.Abstractions;

namespace Api.Endpoints.Server.Tiers.Get;

public sealed record GetTiersResponse(
    List<ServerTier> Tiers);
