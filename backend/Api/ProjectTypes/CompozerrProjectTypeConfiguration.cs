using Api.Abstractions;

namespace Api.ProjectTypes;

public class CompozerrProjectTypeConfiguration : IProjectTypeConfiguration
{
    public ProjectType ProjectType => ProjectType.Compozerr;

    public string[] AllowedSystemTypes => ["Frontend", "Backend"];

    public EnvironmentFileConfig[] EnvironmentFiles =>
    [
        new("Frontend", "frontend/.env", "Frontend"),
        new("Backend", "backend/.env", "Backend")
    ];

    public string DefaultSystemType => "Frontend";
}
