namespace Api.Data;

public class Module : BaseEntityWithId<ModuleId>
{
    public required string Name { get; set; }
    public required Uri RepoUri { get; set; }
}