using Auth.Services;
using Cli.Endpoints.Modules.Create;
using Cli.Endpoints.Projects;
using Core.MediatR;
using Github.Endpoints.SetDefaultInstallationId;
using Github.Services;
using MediatR;
using Octokit;

namespace Cli.Endpoints.Repos;

public sealed record CreateRepoCommandHandler(
    IGithubService GithubService,
    ICurrentUserAccessor CurrentUserAccessor,
    IMediator Mediator) : ICommandHandler<CreateRepoCommand, CreateRepoResponse>
{
    public async Task<CreateRepoResponse> Handle(CreateRepoCommand command, CancellationToken cancellationToken = default)
    {
        var userId = CurrentUserAccessor.CurrentUserId!;

        var clientResponse = await GithubService.GetInstallationClientByUserDefaultAsync(
            userId,
            command.Type);

        var userInstallations = await GithubService.GetInstallationsForUserAsync(userId);

        var currentInstallation = userInstallations.Single(
            userInstallation => userInstallation.InstallationId == clientResponse.InstallationId);

        var response = command.Type switch
        {
            DefaultInstallationIdSelectionType.Projects => await clientResponse.InstallationClient.Repository.Generate(
                "compozerr",
                "base",
                new(command.Name)
                {
                    Private = true,
                    Description = "Created by compozerr.com",
                    Owner = currentInstallation.Name
                }
            ),
            DefaultInstallationIdSelectionType.Modules => await Task.Run(
                async () =>
                    {
                        var forkedRepo = await clientResponse.InstallationClient.Repository.Forks.Create(
                            "compozerr",
                            "template",
                            new NewRepositoryFork()
                            {
                                Organization = currentInstallation.Name
                            }
                        );

                        return await ConflictingRepoRetryOperation(
                            async () =>
                            {
                                var repo = await clientResponse.InstallationClient.Repository.Edit(
                                currentInstallation.Name,
                                forkedRepo.Name,
                                new RepositoryUpdate()
                                {
                                    Name = command.Name,
                                    Description = "Created by compozerr.com",
                                });

                                return repo;
                            });
                    }),
            _ => throw new ArgumentOutOfRangeException(nameof(command.Type), command.Type, null)
        };

        var cloneUrl = $"https://x-access-token:{clientResponse.InstallationToken}@github.com/{response.FullName}.git";
        var gitUrl = $"https://github.com/{response.FullName}.git";

        string? projectId = null;
        switch (command.Type)
        {
            case DefaultInstallationIdSelectionType.Projects:
                var createProjectResponse = await Mediator.Send(
                               new CreateProjectCommand(
                                   command.Name,
                                   gitUrl,
                                   command.LocationIsoCode),
                               cancellationToken);

                projectId = createProjectResponse.ProjectId.Value.ToString();
                break;
            case DefaultInstallationIdSelectionType.Modules:
                var createModuleResponse = await Mediator.Send(
                    new CreateModuleCommand(
                        command.Name,
                        gitUrl)
                    , cancellationToken);
                break;
        }

        return new CreateRepoResponse(
            cloneUrl,
            gitUrl,
            response.Name,
            projectId);
    }

    private static async Task<T> ConflictingRepoRetryOperation<T>(Func<Task<T>> operation, int maxRetries = 3, int initialDelayMs = 1000)
    {
        int retryCount = 0;
        while (true)
        {
            try
            {
                return await operation();
            }
            catch (ApiException ex) when (ex.HttpResponse.Body.ToString()?.Contains("A conflicting repository operation is still in progress") ?? false && retryCount < maxRetries)
            {
                retryCount++;
                var delayMs = initialDelayMs * (1 << (retryCount - 1)); // Exponential backoff
                await Task.Delay(delayMs);
            }
            catch (Exception)
            {
                throw;
            }
        }
    }
}