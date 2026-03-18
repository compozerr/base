using Auth.Services;
using Core.MediatR;
using Organizations.Data.Repositories;
using Organizations.Services;

namespace Organizations.Endpoints.Update;

public sealed class UpdateOrganizationCommandHandler(
    IOrganizationRepository organizationRepository,
    IOrganizationAuthorizationService authService,
    ICurrentUserAccessor currentUserAccessor) : ICommandHandler<UpdateOrganizationCommand, UpdateOrganizationResponse>
{
    public async Task<UpdateOrganizationResponse> Handle(UpdateOrganizationCommand command, CancellationToken cancellationToken)
    {
        var userId = currentUserAccessor.CurrentUserId!;
        if (!await authService.IsAdminAsync(command.OrganizationId, userId, cancellationToken))
            throw new UnauthorizedAccessException("Only admins can update the organization.");

        var org = await organizationRepository.GetByIdAsync(command.OrganizationId, cancellationToken)
            ?? throw new InvalidOperationException("Organization not found.");

        org.Name = command.Name;
        if (command.AvatarUrl is not null)
            org.AvatarUrl = command.AvatarUrl;

        await organizationRepository.UpdateAsync(org, cancellationToken);

        return new UpdateOrganizationResponse(org.Id.Value, org.Name);
    }
}
