namespace Api.Endpoints.Projects.Services.ExposeTcp;

public sealed record ExposeTcpResponse(
    int AllocatedPort,
    string Host);
