using Core.MediatR;

namespace Api.Hosting.VMPooling.Core.VMPoolItemLookup;


public sealed class VMPoolItemLookupRequestHandler() : ICommandHandler<VMPoolItemLookupRequest, VMPoolItemLookupResponse>
{
	public Task<VMPoolItemLookupResponse> Handle(
		VMPoolItemLookupRequest command,
		CancellationToken cancellationToken = default)
	{
		throw new NotImplementedException();
	}
}