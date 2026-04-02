using Api.Data.Repositories;
using Core.MediatR;

namespace Api.Endpoints.Projects.Environments.List;

public sealed class GetEnvironmentsCommandHandler(
    IEnvironmentRepository environmentRepository) : ICommandHandler<GetEnvironmentsCommand, GetEnvironmentsResponse>
{
    public async Task<GetEnvironmentsResponse> Handle(
        GetEnvironmentsCommand command,
        CancellationToken cancellationToken = default)
    {
        var environments = await environmentRepository.GetByProjectIdAsync(
            command.ProjectId,
            cancellationToken);

        var dtos = environments.Select(e => new GetEnvironmentDto(
            e.Id.Value,
            e.Name,
            e.Branch,
            e.Type,
            e.Status,
            e.VmIdentifier,
            e.Subdomain,
            e.AutoDestroy,
            e.TtlDays,
            e.CreatedAtUtc)).ToList();

        return new GetEnvironmentsResponse(dtos);
    }
}
