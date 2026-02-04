using Api.Abstractions;

namespace Api.ProjectTypes;

public class DockerComposeProjectTypeConfiguration : IProjectTypeConfiguration
{
    public ProjectType ProjectType => ProjectType.DockerCompose;

    public string[] AllowedSystemTypes => ["General"];

    public EnvironmentFileConfig[] EnvironmentFiles =>
    [
        new("General", ".env", "Environment")
    ];

    public string DefaultSystemType => "General";

    public string StartCommand => "docker compose up --build --remove-orphans -d";
}
