namespace Api.Data;

public class Project : BaseEntityWithId<ProjectId>
{
    public required string Name { get; set; }
    public required Uri RepoUri { get; set; }
}