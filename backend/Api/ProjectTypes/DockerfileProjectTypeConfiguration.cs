using Api.Abstractions;

namespace Api.ProjectTypes;

public class DockerfileProjectTypeConfiguration : IProjectTypeConfiguration
{
    public ProjectType ProjectType => ProjectType.Dockerfile;

    public string[] AllowedSystemTypes => ["General"];

    public EnvironmentFileConfig[] EnvironmentFiles =>
    [
        new("General", ".env", "Environment")
    ];

    public string DefaultSystemType => "General";
}
