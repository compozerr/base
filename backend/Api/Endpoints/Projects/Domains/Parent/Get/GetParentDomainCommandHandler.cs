using Api.Data.Repositories;
using Core.MediatR;

namespace Api.Endpoints.Projects.Domains.Parent.Get;

public sealed class GetParentDomainCommandHandler(
    IDomainRepository domainRepository) : ICommandHandler<GetParentDomainCommand, GetParentDomainResponse>
{
    public async Task<GetParentDomainResponse> Handle(
        GetParentDomainCommand command,
        CancellationToken cancellationToken = default)
    {
        var parentDomain = await domainRepository.GetParentDomainAsync(command.DomainId, cancellationToken);

        return new(parentDomain.GetValue);
    }
}
