using Api.Data;

namespace Api.Hosting.Dtos;

public static class ProjectUsageDtoExtensions
{
    public static ProjectUsage ToEntity(this ProjectUsageDto dto)
    {
        return new ProjectUsage
        {
            VmId = dto.VmId,
            Name = dto.Name,
            Status = ParseStatus(dto.Status),
            CpuUsagePercentage = dto.CpuUsage,
            CpuCount = dto.CpuCount,
            MemoryUsageGb = dto.MemoryUsedGB,
            TotalMemoryGb = dto.MemoryGB,
            DiskUsageGb = dto.DiskUsedGB,
            TotalDiskGb = dto.DiskGB,
            NetworkInBytesPerSec = dto.NetworkInBytesPerSec,
            NetworkOutBytesPerSec = dto.NetworkOutBytesPerSec,
            DiskReadBytesPerSec = dto.DiskReadBytesPerSec,
            DiskWriteBytesPerSec = dto.DiskWriteBytesPerSec
        };
    }

    private static ProjectStatus ParseStatus(string status)
    {
        return status.ToLower() switch
        {
            "running" => ProjectStatus.Running,
            "stopped" => ProjectStatus.Stopped,
            _ => ProjectStatus.Unknown
        };
    }
}