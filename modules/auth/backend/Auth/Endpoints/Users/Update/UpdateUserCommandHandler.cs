using Auth.Data;
using Core.Extensions;
using Core.MediatR;

namespace Auth.Endpoints.Users.Update;

public class UpdateUserCommandHandler(AuthDbContext authDbContext) : ICommandHandler<UpdateUserCommand, UpdateUserResponse>
{
    public async Task<UpdateUserResponse> Handle(UpdateUserCommand command, CancellationToken cancellationToken = default)
    {
        var user = await authDbContext.Users.FindAsync(command.UserId, cancellationToken);
        user.ThrowIfNull("User not found");

        user.Name = command.Name;
        user.Email = command.Email;
        user.AvatarUrl = command.AvatarUrl;

        await authDbContext.SaveChangesAsync(cancellationToken);

        return new UpdateUserResponse(user.Id);
    }
}