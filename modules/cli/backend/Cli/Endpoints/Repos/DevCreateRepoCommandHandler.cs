using System.Diagnostics;
using Auth.Services;
using Cli.Endpoints.Modules.Create;
using Cli.Endpoints.Projects;
using Core.MediatR;
using Github.Endpoints.SetDefaultInstallationId;
using MediatR;
using Serilog;

namespace Cli.Endpoints.Repos;

public sealed record DevCreateRepoCommandHandler(
    ICurrentUserAccessor CurrentUserAccessor,
    IMediator Mediator) : ICommandHandler<CreateRepoCommand, CreateRepoResponse>
{
    private static readonly string DevReposRoot = Path.Combine(Path.GetTempPath(), "compozerr-dev-repos");

    public async Task<CreateRepoResponse> Handle(CreateRepoCommand command, CancellationToken cancellationToken = default)
    {
        Log.Information("[Cli] DevCreateRepoCommandHandler creating local bare repo for {Name}", command.Name);

        var bareRepoPath = Path.Combine(DevReposRoot, $"{command.Name}.git");
        Directory.CreateDirectory(DevReposRoot);

        // Create bare repo with main as default branch
        await RunGitAsync($"init --bare --initial-branch=main \"{bareRepoPath}\"");

        // Create temp working copy, seed it, push to bare repo
        var workDir = Path.Combine(Path.GetTempPath(), $"compozerr-dev-init-{Guid.NewGuid():N}");
        Directory.CreateDirectory(workDir);

        try
        {
            await RunGitAsync("init", workDir);
            await RunGitAsync("config user.email \"dev@compozerr.local\"", workDir);
            await RunGitAsync("config user.name \"Compozerr Dev\"", workDir);
            await RunGitAsync("checkout -b main", workDir);

            var readmePath = Path.Combine(workDir, "README.md");
            await File.WriteAllTextAsync(readmePath, $"# {command.Name}\n\nCreated by compozerr dev mode.\n", cancellationToken);

            var compozerrJsonPath = Path.Combine(workDir, "compozerr.json");
            await File.WriteAllTextAsync(compozerrJsonPath, $$"""
                {
                  "type": "project",
                  "name": "{{command.Name}}"
                }
                """, cancellationToken);

            await RunGitAsync("add -A", workDir);
            await RunGitAsync("commit -m \"Initial commit (dev mode)\"", workDir);
            await RunGitAsync($"remote add origin \"{bareRepoPath}\"", workDir);
            await RunGitAsync("push -u origin main", workDir);
        }
        finally
        {
            try { Directory.Delete(workDir, true); } catch { /* best effort cleanup */ }
        }

        var cloneUrl = $"file://{bareRepoPath}";
        var gitUrl = cloneUrl;

        string? projectId = null;
        switch (command.Type)
        {
            case DefaultInstallationIdSelectionType.Projects:
                var createProjectResponse = await Mediator.Send(
                    new CreateProjectCommand(
                        command.Name,
                        gitUrl,
                        command.LocationIsoCode,
                        command.Tier),
                    cancellationToken);

                projectId = createProjectResponse.ProjectId.Value.ToString();
                break;

            case DefaultInstallationIdSelectionType.Modules:
                await Mediator.Send(
                    new CreateModuleCommand(command.Name, gitUrl),
                    cancellationToken);

                projectId = command.ProjectId?.Value.ToString();
                break;
        }

        Log.Information("[Cli] Dev repo created at {Path}", bareRepoPath);

        return new CreateRepoResponse(
            cloneUrl,
            gitUrl,
            command.Name,
            projectId);
    }

    private static async Task RunGitAsync(string arguments, string? workingDirectory = null)
    {
        var psi = new ProcessStartInfo("git", arguments)
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
        };

        if (workingDirectory is not null)
            psi.WorkingDirectory = workingDirectory;

        using var process = Process.Start(psi)
            ?? throw new InvalidOperationException($"Failed to start git {arguments}");

        await process.WaitForExitAsync();

        if (process.ExitCode != 0)
        {
            var stderr = await process.StandardError.ReadToEndAsync();
            throw new InvalidOperationException($"git {arguments} failed (exit {process.ExitCode}): {stderr}");
        }
    }
}
