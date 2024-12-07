using Auth.Repositories;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;

namespace Auth.Endpoints.Users.Create;

public class CreateUserCommandValidator : AbstractValidator<CreateUserCommand>
{
    public CreateUserCommandValidator(IServiceScopeFactory scopeFactory)
    {
        var scope = scopeFactory.CreateScope();
        var userRepository = scope.ServiceProvider.GetRequiredService<IUserRepository>();

        RuleFor(x => x.AuthProviderUserId).NotEmpty();
        RuleFor(x => x.Name).NotEmpty();
        RuleFor(x => x.Email).NotEmpty().EmailAddress();
        RuleFor(x => x.AvatarUrl).NotEmpty();
        RuleFor(x => x.Email).MustAsync(async (email, cancellationToken) =>
        {
            var userWithEmailAlreadyExists = await userRepository.ExistsByEmailAsync(email, cancellationToken);
            return !userWithEmailAlreadyExists;
        }).WithMessage("User with this email already exists");
    }
}