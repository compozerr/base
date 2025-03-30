using Api.Data.Repositories;
using Core.MediatR;

namespace Api.Endpoints.Projects.Domains.Delete;

public sealed class DeleteDomainCommandHandler(
    IDomainRepository domainRepository) : ICommandHandler<DeleteDomainCommand, DeleteDomainResponse>
{
    public async Task<DeleteDomainResponse> Handle(
        DeleteDomainCommand command,
        CancellationToken cancellationToken = default)
    {
        await domainRepository.DeleteAsync(
            command.DomainId,
            cancellationToken);

        return new();
    }
}
