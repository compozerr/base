using Api.Abstractions;
using Organizations.Abstractions;

namespace Stripe.Services;

public interface ISubscriptionTransferService
{
    Task<SubscriptionTransferResult> TransferAsync(
        ProjectId projectId,
        OrganizationId fromOrganizationId,
        OrganizationId toOrganizationId,
        CancellationToken cancellationToken = default);
}

public sealed record SubscriptionTransferResult(
    string? NewSubscriptionId,
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
