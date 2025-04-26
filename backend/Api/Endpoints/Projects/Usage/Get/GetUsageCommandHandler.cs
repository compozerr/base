using Api.Data.Repositories;
using Core.MediatR;

namespace Api.Endpoints.Projects.Usage.Get;

public sealed class GetUsageCommandHandler(
    IProjectUsageRepository projectUsageRepository) : ICommandHandler<GetUsageCommand, GetUsageResponse>
{
    public async Task<GetUsageResponse> Handle(GetUsageCommand command, CancellationToken cancellationToken = default)
    {
        
        return new GetUsageResponse();
    }
}
