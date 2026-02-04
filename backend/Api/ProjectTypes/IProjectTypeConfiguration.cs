using Api.Abstractions;
using Api.Data;

namespace Api.ProjectTypes;

public interface IProjectTypeConfiguration
{
    ProjectType ProjectType { get; }
    string[] AllowedSystemTypes { get; }
    EnvironmentFileConfig[] EnvironmentFiles { get; }
    string DefaultSystemType { get; }

    /// <summary>
    /// The default command to start the service. Can be overridden by compozerr.json.
    /// </summary>
    string StartCommand { get; }
}

public record EnvironmentFileConfig(
    string SystemType,
    string FilePath,
    string DisplayName);
