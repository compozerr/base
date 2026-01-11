namespace Api.Data;

public sealed class ExternalDomain : Domain
{
    public ExternalDomain()
    {
        Type = DomainType.External;
    }

    public required string Value { get; set; }
    public required bool IsVerified { get; set; } = false;

    // Verification tracking fields for automated background checks
    public int VerificationCheckCount { get; set; } = 0;
    public DateTime? LastVerificationCheckUtc { get; set; }
    public DateTime? NextVerificationCheckUtc { get; set; }
    public bool HealthCheckPassed { get; set; } = false;

    public override string GetValue => Value;
}