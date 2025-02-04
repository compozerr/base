namespace Api.Endpoints.Server;

public sealed record UpdateServerRequest(
    string Secret,
    string IsoCountryCode,
    string MachineId,
    string Ram,
    string VCpu,
    string Ip);