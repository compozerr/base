namespace Api.Data;

public sealed record ProjectTypeConfigurationDto(
    string[] AllowedSystemTypes,
    EnvironmentFileConfigDto[] EnvironmentFiles,
    string DefaultSystemType,
    string StartCommand,
    ServiceConfigDto[] DefaultServices);

public sealed record EnvironmentFileConfigDto(
    string SystemType,
    string FilePath,
    string DisplayName);

public sealed record ServiceConfigDto(
    string Name,
    int Port,
    string Protocol);
