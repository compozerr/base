using Api.Data.Repositories;
using Core.MediatR;

namespace Api.Endpoints.Projects.Domains.SetPrimary;

public sealed class SetPrimaryCommandHandler(
    IDomainRepository domainRepository) : ICommandHandler<SetPrimaryCommand, SetPrimaryResponse>
{
    public async Task<SetPrimaryResponse> Handle(SetPrimaryCommand command, CancellationToken cancellationToken = default)
    {
        var domain = await domainRepository.GetByIdAsync(
            command.DomainId,
            cancellationToken);

        var currentDomains = await domainRepository.GetAllAsync(
            x => x.Where(x => x.ProjectId == domain!.ProjectId),
            cancellationToken);

        foreach (var currentDomain in currentDomains)
        {
            currentDomain.IsPrimary = currentDomain.Id == command.DomainId;
        }

        await domainRepository.UpdateRangeAsync(
            currentDomains,
            cancellationToken);

        return new SetPrimaryResponse(true);
    }
}
