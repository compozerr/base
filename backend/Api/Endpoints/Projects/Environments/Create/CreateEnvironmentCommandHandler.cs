using Api.Data;
using Api.Data.Repositories;
using Core.MediatR;

namespace Api.Endpoints.Projects.Environments.Create;

public sealed class CreateEnvironmentCommandHandler(
    IEnvironmentRepository environmentRepository,
    IProjectRepository projectRepository) : ICommandHandler<CreateEnvironmentCommand, CreateEnvironmentResponse>
{
    public async Task<CreateEnvironmentResponse> Handle(
        CreateEnvironmentCommand command,
        CancellationToken cancellationToken = default)
    {
        var project = await projectRepository.GetByIdAsync(command.ProjectId, cancellationToken)
            ?? throw new ArgumentException($"Project with ID {command.ProjectId} not found.");

        var subdomain = GenerateSubdomain(project.Name, command.Name);

        var environment = new Api.Data.Environment
        {
            Id = EnvironmentId.CreateNew(),
            ProjectId = command.ProjectId,
            Name = command.Name,
            Branch = command.Branch,
            Type = command.Type,
            Status = EnvironmentStatus.Active,
            Subdomain = subdomain,
        };

        var created = await environmentRepository.AddAsync(environment, cancellationToken);

        return new CreateEnvironmentResponse(created.Id.Value.ToString());
    }

    private static string GenerateSubdomain(string projectName, string environmentName)
    {
        var sanitized = $"{projectName}-{environmentName}"
            .ToLowerInvariant()
            .Replace(" ", "-");

        // Remove any characters that aren't alphanumeric or hyphens
        sanitized = System.Text.RegularExpressions.Regex.Replace(sanitized, @"[^a-z0-9\-]", "");

        // Trim leading/trailing hyphens and collapse multiple hyphens
        sanitized = System.Text.RegularExpressions.Regex.Replace(sanitized, @"-+", "-").Trim('-');

        // Limit length to fit within subdomain constraints
        if (sanitized.Length > 63)
            sanitized = sanitized[..63].TrimEnd('-');

        return sanitized;
    }
}
