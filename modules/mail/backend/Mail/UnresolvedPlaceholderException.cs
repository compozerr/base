namespace Mail;

public sealed class UnresolvedPlaceholderException(string message) : Exception(message);
