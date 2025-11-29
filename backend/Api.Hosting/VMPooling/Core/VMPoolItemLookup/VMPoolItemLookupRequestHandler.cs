using Api.Abstractions;
using Api.Data.Repositories;
using Core.MediatR;

namespace Api.Hosting.VMPooling.Core.VMPoolItemLookup;


public sealed class VMPoolItemLookupRequestHandler(
    IVMPoolRepository vMPoolRepository) : ICommandHandler<VMPoolItemLookupRequest, VMPoolItemLookupResponse>
{
	public async Task<VMPoolItemLookupResponse> Handle(
		VMPoolItemLookupRequest command,
		CancellationToken cancellationToken = default)
	{
		var vmPoolItemId = await vMPoolRepository.GetFirstVMPoolItemIdOrDefaultAsync(
			command,
			cancellationToken);

		var foundItemId = vmPoolItemId is not null;

		return new VMPoolItemLookupResponse(
			foundItemId,
			vmPoolItemId);
	}
}