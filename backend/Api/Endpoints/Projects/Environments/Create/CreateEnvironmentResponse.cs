namespace Api.Endpoints.Projects.Environments.Create;

public sealed record CreateEnvironmentResponse(Guid Id, string? Subdomain, string Tier, decimal MonthlyPrice);
