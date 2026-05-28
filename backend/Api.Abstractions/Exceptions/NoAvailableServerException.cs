namespace Api.Abstractions.Exceptions;

/// <summary>
/// Thrown when no server is visible/available to the current caller for an
/// allocation (e.g. a non-admin in a region whose only servers are AdminOnly).
///
/// Subclasses <see cref="ArgumentException"/> so the global exception handler
/// maps it to a 400 Bad Request (a controlled validation failure) instead of
/// the unhandled 5xx/conflict that an empty-sequence <c>.First()</c> produced.
/// </summary>
public sealed class NoAvailableServerException(string message) : ArgumentException(message);
