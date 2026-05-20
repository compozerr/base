using Organizations.Abstractions;

namespace Stripe.Services;

public interface ISubscriptionTransferService
{
    /// <summary>
    /// Migrate every subscription in <paramref name="subscriptionIds"/> from the source
    /// org's Stripe customer to the target org's. Returns one result per input id; the
    /// caller must apply <see cref="MigratedSubscription.NewSubscriptionId"/> back onto
    /// the corresponding environment so the link survives the transfer.
    /// </summary>
    Task<IReadOnlyList<MigratedSubscription>> TransferAsync(
        IReadOnlyList<string> subscriptionIds,
        OrganizationId fromOrganizationId,
        OrganizationId toOrganizationId,
        CancellationToken cancellationToken = default);
}

public sealed record MigratedSubscription(
    string OldSubscriptionId,
    string? NewSubscriptionId,
    string? NewStatus,
    bool WasMigrated);

/// <summary>
/// Inherits from <see cref="InvalidOperationException"/> so the global API
/// ExceptionHandler maps it to 409 Conflict (see <c>Core/Helpers/ExceptionHandler.cs</c>).
/// The frontend's ApiErrorHandler then surfaces the message as a destructive toast.
/// </summary>
public class SubscriptionTransferException : InvalidOperationException
{
    public SubscriptionTransferException(string message) : base(message) { }
    public SubscriptionTransferException(string message, Exception inner) : base(message, inner) { }
}
