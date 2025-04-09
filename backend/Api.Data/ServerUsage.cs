namespace Api.Data;

public sealed record ServerUsage 
{
    public decimal AvgRamPercentage { get; set; } = 0;
    public decimal AvgCpuPercentage { get; set; } = 0;
    public decimal AvgDiskPercentage { get; set; } = 0;
}