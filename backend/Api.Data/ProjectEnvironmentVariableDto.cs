namespace Api.Data;

public sealed record ProjectEnvironmentVariableDto(
    SystemType SystemType,
    string Key,
    string Value);