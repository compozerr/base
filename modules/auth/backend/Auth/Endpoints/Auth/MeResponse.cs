namespace Auth.Endpoints.Auth;

internal sealed record MeResponse(
    Guid Id,
    string Name,
    string Email,
    string AvatarUrl);