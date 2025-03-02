namespace Api.Data;

public class ProjectEnvironmentVariable : BaseEntityWithId<ProjectEnvironmentVariableId>
{
    public ProjectEnvironmentId? ProjectEnvironmentId { get; set; }
    public required string Key { get; set; }
    public required string Value { get; set; }
}