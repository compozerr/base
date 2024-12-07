namespace Auth.Endpoints.Auth;

internal sealed record MeResponse(
    Guid UserId,
    string Name,
    string Email,
    string AvatarUrl);