using Microsoft.AspNetCore.Builder;

namespace Auth.Endpoints.Users;

public class UsersGroup : CarterModule
{
    public const string UsersEndpoint = "/users";
    public UsersGroup() : base(UsersEndpoint)
    {
        WithTags(nameof(Users));
        RequireAuthorization();
    }

    public override void AddRoutes(IEndpointRouteBuilder app)
    {
        app.AddUsersRoute().RequireAuthorization("users:read");
        // app.AddCreateUserRoute();
        // app.AddUpdateUserRoute();
        // app.AddDeleteUserRoute();
    }

}