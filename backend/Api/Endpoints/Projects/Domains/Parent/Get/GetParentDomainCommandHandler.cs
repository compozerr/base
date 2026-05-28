using Api.Abstractions.Exceptions;
using Api.Data.Repositories;
using Api.Hosting.Services.EnvironmentTargetResolver;
using Core.MediatR;

namespace Api.Endpoints.Projects.Domains.Parent.Get;

public sealed class GetParentDomainCommandHandler(
    IDomainRepository domainRepository,
    IEnvironmentTargetResolver environmentTargetResolver) : ICommandHandler<GetParentDomainCommand, GetParentDomainResponse>
{
    public async Task<GetParentDomainResponse> Handle(
        GetParentDomainCommand command,
        CancellationToken cancellationToken = default)
    {
        var parentDomain = await domainRepository.GetParentDomainAsync(command.DomainId, cancellationToken);

        string? serverIp = null;
        try
        {
            var server = await environmentTargetResolver.GetServerForDomainAsync(parentDomain.Id, cancellationToken);
            serverIp = server.Ip;
        }
        catch (ServerNotFoundException)
        {
            // No server allocated for this domain's env (or any fallback) —
            // the response carries a null IP so the UI can render the
            // "no server" state. This matches the prior behaviour of
            // `domainWithServer?.Project?.Server?.Ip` returning null when no
            // server was attached.
        }

        return new(parentDomain.GetValue, serverIp);
    }
}
