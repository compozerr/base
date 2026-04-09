using MediatR;

namespace Auth.Abstractions;

public record UserLoggedInNotification(UserId UserId, string GithubUsername) : INotification;
