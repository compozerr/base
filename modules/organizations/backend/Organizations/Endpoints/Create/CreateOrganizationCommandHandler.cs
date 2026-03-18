using Auth.Services;
using Core.MediatR;
using Organizations.Data.Repositories;
using Organizations.Models;

namespace Organizations.Endpoints.Create;

public sealed class CreateOrganizationCommandHandler(
    IOrganizationRepository organizationRepository,
    IOrganizationMemberRepository memberRepository,
    ICurrentUserAccessor currentUserAccessor) : ICommandHandler<CreateOrganizationCommand, CreateOrganizationResponse>
{
    public async Task<CreateOrganizationResponse> Handle(CreateOrganizationCommand command, CancellationToken cancellationToken)
    {
        var userId = currentUserAccessor.CurrentUserId!;

        var org = new Organization
        {
            Name = command.Name,
            Slug = command.Slug.ToLowerInvariant(),
            IsPersonal = false,
            OwnerUserId = userId
        };

        await organizationRepository.AddAsync(org, cancellationToken);

        var member = new OrganizationMember
        {
            OrganizationId = org.Id,
            UserId = userId,
            Role = OrganizationRole.Admin
        };

        await memberRepository.AddAsync(member, cancellationToken);

        return new CreateOrganizationResponse(org.Id.Value, org.Name, org.Slug);
    }
}
