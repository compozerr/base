using Api.Data.Repositories;
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
    IProjectRepository ProjectRepository,
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
                        var project = await ProjectRepository.GetByIdAsync(
                            command.ProjectId!,
                            cancellationToken) ?? throw new Exception("Project not found");

                        var forkedRepo = await GithubService.ForkRepositoryAsync(clientResponse.InstallationClient,
                            "compozerr",
                            "template",
                            currentInstallation.Name,
                            command.Name);

                        await GithubService.CreateBranchAsync(
                            clientResponse.InstallationClient,
                            currentInstallation.Name,
                            command.Name,
                            project.Name);

                        return forkedRepo;
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

                projectId = command.ProjectId?.Value.ToString();
                break;
        }

        return new CreateRepoResponse(
            cloneUrl,
            gitUrl,
            response.Name,
            projectId);
    }


}