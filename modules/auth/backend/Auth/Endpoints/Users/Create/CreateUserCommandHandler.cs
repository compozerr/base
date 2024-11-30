using Auth.Data;
using Auth.Models;
using Core.MediatR;

namespace Auth.Endpoints.Users.Create;

public class CreateUserCommandHandler(AuthDbContext authDbContext) : ICommandHandler<CreateUserCommand, CreateUserResponse>
{
    public async Task<CreateUserResponse> Handle(CreateUserCommand command, CancellationToken cancellationToken = default)
    {
        var user = new User
        {
            Email = command.Email,
            AvatarUrl = command.AvatarUrl
        };

        var addedUser = await authDbContext.AddAsync(user, cancellationToken);

        await authDbContext.SaveChangesAsync(cancellationToken);

        return new CreateUserResponse(addedUser.Entity.Id);
    }
}