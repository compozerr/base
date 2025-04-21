using Api.Abstractions;
using Api.Data;

namespace Api.Hosting.Dtos;

public static class ProjectUsageDtoMapping
{
    public static ProjectUsage ToEntity(this ProjectUsageDto dto)
    {
        return new ProjectUsage
        {
            ProjectId = ProjectId.Parse(dto.Name),
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

    public static List<ProjectUsage> ToEntities(this IEnumerable<ProjectUsageDto> dtos)
    {
        return [.. dtos.Select(dto => dto.ToEntity())];
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