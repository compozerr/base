namespace Api.Data;

public enum ProjectStatus
{
    Unknown = 0,
    Running = 1,
    Stopped = 2,
}

public class ProjectUsage : BaseEntityWithId<ProjectUsageId>
{
    public required int VmId { get; set; }
    public required string Name { get; set; }
    public required ProjectStatus Status { get; set; }
    public required decimal CpuUsagePercentage { get; set; }
    public required int CpuCount { get; set; }
    public required decimal MemoryUsageGb { get; set; }
    public required decimal TotalMemoryGb { get; set; }
    public required decimal DiskUsageGb { get; set; }
    public required decimal TotalDiskGb { get; set; }
    public required decimal NetworkInBytesPerSec { get; set; }
    public required decimal NetworkOutBytesPerSec { get; set; }
    public required decimal DiskReadBytesPerSec { get; set; }
    public required decimal DiskWriteBytesPerSec { get; set; }
}