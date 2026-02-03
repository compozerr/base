using Api.Abstractions;

namespace Api.ProjectTypes;

public class N8nProjectTypeConfiguration : IProjectTypeConfiguration
{
    public ProjectType ProjectType => ProjectType.N8n;

    public string[] AllowedSystemTypes => ["General"];

    public EnvironmentFileConfig[] EnvironmentFiles =>
    [
        new("General", ".env", "Environment")
    ];

    public string DefaultSystemType => "General";
}
