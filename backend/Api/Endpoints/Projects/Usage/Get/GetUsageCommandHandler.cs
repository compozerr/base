using Api.Data.Repositories;
using Core.MediatR;

namespace Api.Endpoints.Projects.Usage.Get;

public sealed class GetUsageCommandHandler(
    IProjectUsageRepository projectUsageRepository) : ICommandHandler<GetUsageCommand, GetUsageResponse>
{
    public async Task<GetUsageResponse> Handle(GetUsageCommand command, CancellationToken cancellationToken = default)
    {
        var data = command.UsageSpan switch
        {
            UsageSpan.Day => await projectUsageRepository.GetDay(command.ProjectId),
            UsageSpan.Week => await projectUsageRepository.GetWeek(command.ProjectId),
            UsageSpan.Month => await projectUsageRepository.GetMonth(command.ProjectId),
            UsageSpan.Year => await projectUsageRepository.GetYear(command.ProjectId),
            UsageSpan.Total => await projectUsageRepository.GetTotal(command.ProjectId),
            _ => await projectUsageRepository.GetTotal(command.ProjectId)
        };

        var points = new Dictionary<UsagePointType, List<UsagePoint>>
        {
            { UsagePointType.CPU, [] },
            { UsagePointType.Ram, [] },
            { UsagePointType.DiskWrite, [] },
            { UsagePointType.DiskRead, [] },
            { UsagePointType.NetworkIn, [] },
            { UsagePointType.NetworkOut, [] }
        };

        foreach (var point in data)
        {
            points[UsagePointType.CPU].Add(new UsagePoint(point.CreatedAtUtc, point.CpuUsagePercentage));
            points[UsagePointType.Ram].Add(new UsagePoint(point.CreatedAtUtc, point.MemoryUsageGb));
            points[UsagePointType.DiskWrite].Add(new UsagePoint(point.CreatedAtUtc, point.DiskWriteBytesPerSec));
            points[UsagePointType.DiskRead].Add(new UsagePoint(point.CreatedAtUtc, point.DiskReadBytesPerSec));
            points[UsagePointType.NetworkIn].Add(new UsagePoint(point.CreatedAtUtc, point.NetworkInBytesPerSec));
            points[UsagePointType.NetworkOut].Add(new UsagePoint(point.CreatedAtUtc, point.NetworkOutBytesPerSec));
        }

        return new GetUsageResponse(
            points,
            command.UsageSpan);
    }
}
