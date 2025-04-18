using System.Text.Json.Serialization;

namespace Api.Data;

public sealed record ServerUsage
{

    [JsonPropertyName("ramPercentage")]
    public decimal? AvgRamPercentage { get; set; } = 0;
    [JsonPropertyName("cpuPercentage")]
    public decimal? AvgCpuPercentage { get; set; } = 0;
    [JsonPropertyName("diskPercentage")]
    public decimal? AvgDiskPercentage { get; set; } = 0;
}