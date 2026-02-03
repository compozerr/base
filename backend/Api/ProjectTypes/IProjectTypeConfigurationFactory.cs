using Api.Abstractions;

namespace Api.ProjectTypes;

public interface IProjectTypeConfigurationFactory
{
    IProjectTypeConfiguration GetConfiguration(ProjectType type);
}
