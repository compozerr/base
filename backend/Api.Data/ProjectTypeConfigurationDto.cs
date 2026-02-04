namespace Api.Data;

public sealed record ProjectTypeConfigurationDto(
    string[] AllowedSystemTypes,
    EnvironmentFileConfigDto[] EnvironmentFiles,
    string DefaultSystemType,
    string StartCommand);

public sealed record EnvironmentFileConfigDto(
    string SystemType,
    string FilePath,
    string DisplayName);
