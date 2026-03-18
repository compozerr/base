using System.Text.RegularExpressions;
using Auth.Abstractions;
using Auth.Repositories;
using Core.Abstractions;
using Organizations.Data.Repositories;
using Organizations.Models;
using Serilog;

namespace Organizations.Events;

public sealed partial class CreatePersonalOrg_UserCreatedEventHandler(
    IOrganizationRepository organizationRepository,
    IOrganizationMemberRepository memberRepository,
    IUserRepository userRepository) : EntityDomainEventHandlerBase<UserCreatedEvent>
{
    private static readonly ILogger Logger = Log.ForContext<CreatePersonalOrg_UserCreatedEventHandler>();

    protected override async Task HandleAfterSaveAsync(UserCreatedEvent domainEvent, CancellationToken cancellationToken)
    {
        // Check if personal org already exists
        var existingOrg = await organizationRepository.GetPersonalOrgForUserAsync(domainEvent.UserId, cancellationToken);
        if (existingOrg is not null)
            return;

        var user = await userRepository.GetByIdAsync(domainEvent.UserId, cancellationToken);
        if (user is null)
        {
            Logger.Error("User {UserId} not found when creating personal organization", domainEvent.UserId);
            return;
        }

        var slug = GenerateSlug(user.Name, domainEvent.UserId);

        var org = new Organization
        {
            Name = user.Name,
            Slug = slug,
            IsPersonal = true,
            OwnerUserId = domainEvent.UserId,
            AvatarUrl = user.AvatarUrl
        };

        await organizationRepository.AddAsync(org, cancellationToken);

        var member = new OrganizationMember
        {
            OrganizationId = org.Id,
            UserId = domainEvent.UserId,
            Role = OrganizationRole.Admin
        };

        await memberRepository.AddAsync(member, cancellationToken);

        Logger.Information("Created personal organization {OrgId} for user {UserId}", org.Id, domainEvent.UserId);
    }

    private static string GenerateSlug(string name, UserId userId)
    {
        var baseSlug = SlugRegex().Replace(name.ToLowerInvariant(), "-").Trim('-');
        if (string.IsNullOrEmpty(baseSlug))
            baseSlug = "user";

        // Append a short portion of the user ID for uniqueness
        var shortId = userId.Value.ToString("N")[..8];
        return $"{baseSlug}-{shortId}";
    }

    [GeneratedRegex("[^a-z0-9]+")]
    private static partial Regex SlugRegex();
}
