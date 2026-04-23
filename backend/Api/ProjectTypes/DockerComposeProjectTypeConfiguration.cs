using Api.Abstractions;
using Api.Data.ProjectTypes;

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

    public string StartCommand => "COMPOSE_HTTP_TIMEOUT=600 docker-compose up --build --remove-orphans -d";

    public ServiceConfig[] DefaultServices =>
    [
        new("App", 1234)
    ];
}
