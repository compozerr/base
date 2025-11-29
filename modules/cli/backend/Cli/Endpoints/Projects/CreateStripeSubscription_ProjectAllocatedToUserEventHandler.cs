using Cli.Abstractions;
using Core.Abstractions;
using MediatR;
using Stripe.Endpoints.Subscriptions.UpsertSubscription;

namespace Cli.Endpoints.Projects;

public sealed class CreateStripeSubscription_ProjectAllocatedToUserEventHandler(
    ISender sender) : IEventHandler<ProjectAllocatedToUserEvent>
{
    public async Task Handle(
        ProjectAllocatedToUserEvent notification,
        CancellationToken cancellationToken)
    {
        var command = new UpsertSubscriptionCommand(
            notification.Entity.Id,
            notification.Entity.ServerTierId,
            GetCouponCodeIfApplicable(notification));

        await sender.Send(
            command,
            cancellationToken);
    }

    private static string? GetCouponCodeIfApplicable(ProjectAllocatedToUserEvent notification)
    {
        if (notification.Entity.Type == Api.Abstractions.ProjectType.N8n)
        {
            return "BLACKFRIDAY2025";
        }

        return null;
    }
}
