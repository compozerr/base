using Auth.Repositories;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;

namespace Auth.Endpoints.Users.Update;

public class UpdateUserCommandValidator : AbstractValidator<UpdateUserCommand>
{
    public UpdateUserCommandValidator(IServiceScopeFactory scopeFactory)
    {
        var scope = scopeFactory.CreateScope();
        var userRepository = scope.ServiceProvider.GetRequiredService<IUserRepository>();

        RuleFor(x => x.UserId).NotEmpty();
        RuleFor(x => x.Name).NotEmpty();
        RuleFor(x => x.Email).NotEmpty().EmailAddress();
        RuleFor(x => x.AvatarUrl).NotEmpty();
        RuleFor(x => x.Email).MustAsync(async (email, cancellationToken) =>
        {
            var user = await userRepository.GetByEmailAsync(email, cancellationToken);
            return user is null;
        }).WithMessage("User with this email already exists");
    }
}