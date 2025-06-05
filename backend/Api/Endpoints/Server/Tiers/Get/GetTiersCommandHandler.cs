using Api.Abstractions;
using Core.MediatR;

namespace Api.Endpoints.Server.Tiers.Get;

public sealed class GetTiersCommandHandler : ICommandHandler<GetTiersCommand, GetTiersResponse>
{
	public Task<GetTiersResponse> Handle(
		GetTiersCommand command,
		CancellationToken cancellationToken = default)
		=> Task.FromResult(
		    new GetTiersResponse(ServerTiers.All));
}
