namespace Auth.Endpoints.Users.Create;

public sealed record CreateUserRequest(
    string Email,
    string AvatarUrl);