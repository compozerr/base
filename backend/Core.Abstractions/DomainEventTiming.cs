namespace Core.Abstractions;

/// <summary>
/// Specifies when a domain event should be dispatched relative to SaveChanges.
/// </summary>
public enum DomainEventTiming
{
    /// <summary>
    /// Event is dispatched before SaveChanges is called.
    /// Event handlers can modify entities and those changes will be included in the same SaveChanges.
    /// </summary>
    BeforeSaveChanges,

    /// <summary>
    /// Event is dispatched after SaveChanges completes successfully.
    /// Event handlers have access to generated entity IDs and the committed transaction.
    /// </summary>
    AfterSaveChanges
}
