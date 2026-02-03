namespace Api.Abstractions.Exceptions;

public class GitHubTokenException(string message) : Exception(message);

public sealed class GitHubTokenRequiredException()
    : GitHubTokenException("GitHub not connected. Please connect GitHub in your project settings.");

public sealed class GitHubTokenExpiredException()
    : GitHubTokenException("GitHub token expired or revoked. Please reconnect GitHub in your project settings.");
