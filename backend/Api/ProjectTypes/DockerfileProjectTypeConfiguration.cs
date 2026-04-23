using Api.Abstractions;
using Api.Data.ProjectTypes;

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

    /// <summary>
    /// Builds and runs the Docker container on port 1234 (the standard routing port).
    /// Uses --force-recreate to ensure fresh container state on each deployment.
    /// </summary>
    public string StartCommand => "docker build -t app . && docker rm -f app 2>/dev/null; docker run -d --name app -p 1234:80 app";

    public ServiceConfig[] DefaultServices =>
    [
        new("App", 1234)
    ];
}
