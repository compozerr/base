using Auth.Data;
using Auth.Models;
using Core.MediatR;

namespace Auth.Endpoints.Users.Create;

public class CreateUserCommandHandler(AuthDbContext authDbContext) : ICommandHandler<CreateUserCommand, CreateUserResponse>
{
    public async Task<CreateUserResponse> Handle(CreateUserCommand command, CancellationToken cancellationToken = default)
    {
        var user = await authDbContext.AddAsync(new User
        {
            Email = command.Email,
            AvatarUrl = command.AvatarUrl
        }, cancellationToken);

        await authDbContext.SaveChangesAsync(cancellationToken);

        return new CreateUserResponse(user.Entity.Id);
    }
}