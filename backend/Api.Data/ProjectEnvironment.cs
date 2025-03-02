namespace Api.Data;

public class ProjectEnvironment : BaseEntityWithId<ProjectEnvironmentId>
{
    public required ProjectId ProjectId { get; set; }
    public required List<string> Branches { get; set; }

    public virtual ICollection<ProjectEnvironmentVariable>? ProjectEnvironmentVariables { get; set; }
}