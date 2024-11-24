
using Auth.Models;
using Auth.Repositories;
using Database.Models;
using Database.Repositories;
using Template;

namespace Api.Features.Home;

public class HomeModule : CarterModule
{
    public HomeModule() : base("/")
    {
        WithTags(nameof(HomeModule));
    }

    public override void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapGet("/", (IConfiguration configuration) => $"{ExampleClass.ExampleMethod()} config: {configuration["SOMESECRET"]}");

        app.MapGet("/users/add", async (IConfiguration configuration, IUserRepository userRepository) =>
        {
            var user = await userRepository.AddAsync(new User
            {
                Email = "hey@hey.com",
                Username = "hey",
            });

            return user;
        });

        app.MapGet("/users", async (IConfiguration configuration, IUserRepository userRepository) =>
        {
            var users = await userRepository.GetAllAsync();
            return users;
        });
    }
}