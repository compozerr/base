namespace Auth.Endpoints.Auth;

internal sealed record MeResponse(
    UserId Id,
    string Name,
    string Email,
    string AvatarUrl);