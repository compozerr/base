using System.Text.Json.Serialization;

namespace Api.Hosting.Dtos;

public sealed record ProjectUsageDto
{
    [JsonPropertyName("vmid")]
    public int VmId { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("projectId")]
    public string ProjectId { get; set; } = string.Empty;

    [JsonPropertyName("environmentName")]
    public string? EnvironmentName { get; set; }

    [JsonPropertyName("status")]
    public string Status { get; set; } = string.Empty;
    
    [JsonPropertyName("cpuUsage")]
    public decimal CpuUsage { get; set; }
    
    [JsonPropertyName("cpuCount")]
    public int CpuCount { get; set; }
    
    [JsonPropertyName("memoryGB")]
    public decimal MemoryGB { get; set; }
    
    [JsonPropertyName("memoryUsedGB")]
    public decimal MemoryUsedGB { get; set; }
    
    [JsonPropertyName("diskGB")]
    public decimal DiskGB { get; set; }
    
    [JsonPropertyName("diskUsedGB")]
    public decimal DiskUsedGB { get; set; }
    
    [JsonPropertyName("networkInBytesPerSec")]
    public decimal NetworkInBytesPerSec { get; set; }
    
    [JsonPropertyName("networkOutBytesPerSec")]
    public decimal NetworkOutBytesPerSec { get; set; }
    
    [JsonPropertyName("diskReadBytesPerSec")]
    public decimal DiskReadBytesPerSec { get; set; }
    
    [JsonPropertyName("diskWriteBytesPerSec")]
    public decimal DiskWriteBytesPerSec { get; set; }
    
    [JsonPropertyName("freememGB")]
    public decimal FreeMemoryGB { get; set; }
}