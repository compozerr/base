using Jobs;
using Microsoft.EntityFrameworkCore;
using Organizations.Data;
using Organizations.Models;
using Serilog;

namespace Organizations.Jobs;

public sealed class ExpireInvitationsJob(
    OrganizationsDbContext dbContext) : JobBase<ExpireInvitationsJob>
{
    private static readonly ILogger Logger = Log.ForContext<ExpireInvitationsJob>();

    public override async Task ExecuteAsync()
    {
        var expiredInvitations = await dbContext.OrganizationInvitations
            .Where(i => i.Status == InvitationStatus.Pending && i.ExpiresAtUtc < DateTime.UtcNow)
            .ToListAsync();

        foreach (var invitation in expiredInvitations)
        {
            invitation.Status = InvitationStatus.Expired;
        }

        if (expiredInvitations.Count > 0)
        {
            await dbContext.SaveChangesAsync();
            Logger.Information("Expired {Count} pending invitations", expiredInvitations.Count);
        }
    }
}
