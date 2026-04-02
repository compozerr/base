namespace Api.Endpoints.Projects.Environments.ChangeTier;

public sealed record ChangeEnvironmentTierResponse(string Tier, decimal MonthlyPrice);
