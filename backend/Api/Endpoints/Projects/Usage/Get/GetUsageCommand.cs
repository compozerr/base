using Api.Abstractions;
using Core.MediatR;

namespace Api.Endpoints.Projects.Usage.Get;

public enum UsageSpan
{
    Total = 0,
    Day = 1,
    Week = 2,
    Month = 3,
    Year = 4,
}

public sealed record GetUsageCommand(
    ProjectId ProjectId,
    UsageSpan UsageSpan) : ICommand<GetUsageResponse>;
