using Api.Abstractions;
using Api.Data;

namespace Api.Endpoints.Server;

public sealed record GetServersResponse(List<ServerDto> Servers);

public sealed record ServerDto(
    ServerId Id,
    LocationId? LocationId,
    string? LocationIsoCountryCode,
    ServerVisibility Visibility,
    string HostName,
    string ApiDomain,
    string Ip);
