using Api.Abstractions;
using Api.Data;
using Api.Data.Repositories;
using Core.MediatR;
using Serilog;

namespace Api.Endpoints.Projects.Environments.Create;

public sealed class CreateEnvironmentCommandHandler(
    IProjectEnvironmentRepository environmentRepository,
    IProjectRepository projectRepository) : ICommandHandler<CreateEnvironmentCommand, CreateEnvironmentResponse>
{
    public async Task<CreateEnvironmentResponse> Handle(
        CreateEnvironmentCommand command,
        CancellationToken cancellationToken = default)
    {
        var project = await projectRepository.GetByIdAsync(command.ProjectId, cancellationToken)
            ?? throw new InvalidOperationException($"Project {command.ProjectId} not found.");

        var subdomain = command.Type == EnvironmentType.Preview
            ? GenerateSubdomain(project.Name, command.Branch)
            : null;

        var environment = new ProjectEnvironment
        {
            Id = ProjectEnvironmentId.CreateNew(),
            ProjectId = command.ProjectId,
            Name = command.Name,
            Type = command.Type,
            Branches = [command.Branch],
            AutoDeploy = true,
            Subdomain = subdomain
        };

        await environmentRepository.AddAsync(environment, cancellationToken);

        Log.ForContext(nameof(environment), environment, true)
           .Information("Created {Type} environment '{Name}' for project {ProjectId}",
               command.Type, command.Name, command.ProjectId);

        return new CreateEnvironmentResponse(environment.Id.Value);
    }

    private static string GenerateSubdomain(string projectName, string branch)
    {
        var sanitizedProject = SanitizeForSubdomain(projectName);
        var sanitizedBranch = SanitizeForSubdomain(branch);

        // Limit length to keep subdomain reasonable
        var subdomain = $"{sanitizedBranch}-{sanitizedProject}";
        return subdomain.Length > 63 ? subdomain[..63].TrimEnd('-') : subdomain;
    }

    private static string SanitizeForSubdomain(string input)
    {
        return new string(input
            .ToLowerInvariant()
            .Select(c => char.IsLetterOrDigit(c) ? c : '-')
            .ToArray())
            .Trim('-');
    }
}
