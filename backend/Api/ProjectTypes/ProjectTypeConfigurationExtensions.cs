using Api.Data;

namespace Api.ProjectTypes;

public static class ProjectTypeConfigurationExtensions
{
    public static ProjectTypeConfigurationDto ToDto(this IProjectTypeConfiguration config)
        => new(
            config.AllowedSystemTypes,
            config.EnvironmentFiles.Select(f => new EnvironmentFileConfigDto(
                f.SystemType,
                f.FilePath,
                f.DisplayName)).ToArray(),
            config.DefaultSystemType,
            config.StartCommand);
}
