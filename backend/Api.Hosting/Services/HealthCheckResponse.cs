namespace Api.Hosting.Services;

public enum HealthCheckCode
{
    Healthy = 0,
    Unhealthy = 1,
    NotFound = 2
}

public class HealthCheckResponse
{
    public HealthCheckCode Code { get; set; } = HealthCheckCode.Healthy;
    public string Error { get; set; } = string.Empty;
}