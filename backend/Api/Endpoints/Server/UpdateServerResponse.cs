namespace Api.Endpoints.Server;

public sealed record UpdateServerResponse(bool Success, string? PemPublicKey);