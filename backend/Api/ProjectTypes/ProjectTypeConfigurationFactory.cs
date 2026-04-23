using Api.Abstractions;
using Api.Data.ProjectTypes;

namespace Api.ProjectTypes;

public class ProjectTypeConfigurationFactory : IProjectTypeConfigurationFactory
{
    private readonly Dictionary<ProjectType, IProjectTypeConfiguration> _configurations;

    public ProjectTypeConfigurationFactory(IEnumerable<IProjectTypeConfiguration> configurations)
    {
        _configurations = configurations.ToDictionary(c => c.ProjectType);
    }

    public IProjectTypeConfiguration GetConfiguration(ProjectType type)
        => _configurations.TryGetValue(type, out var config)
            ? config
            : throw new NotSupportedException($"No configuration for project type: {type}");
}
