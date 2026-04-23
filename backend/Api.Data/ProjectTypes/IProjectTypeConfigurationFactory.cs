using Api.Abstractions;

namespace Api.Data.ProjectTypes;

public interface IProjectTypeConfigurationFactory
{
    IProjectTypeConfiguration GetConfiguration(ProjectType type);
}
