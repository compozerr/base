using Api.Data.Repositories;
using Core.MediatR;
using Microsoft.EntityFrameworkCore;

namespace Api.Endpoints.Projects.Domains.Parent.Get;

public sealed class GetParentDomainCommandHandler(
    IDomainRepository domainRepository) : ICommandHandler<GetParentDomainCommand, GetParentDomainResponse>
{
    public async Task<GetParentDomainResponse> Handle(
        GetParentDomainCommand command,
        CancellationToken cancellationToken = default)
    {
        var parentDomain = await domainRepository.GetParentDomainAsync(command.DomainId, cancellationToken);

        // Load the project with server to get IP
        var domainWithServer = await domainRepository.Query()
            .Include(d => d.Project)
            .ThenInclude(p => p!.Server)
            .FirstOrDefaultAsync(d => d.Id == parentDomain.Id, cancellationToken);

        var serverIp = domainWithServer?.Project?.Server?.Ip;

        return new(parentDomain.GetValue, serverIp);
    }
}
