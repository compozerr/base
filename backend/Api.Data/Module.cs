using System.ComponentModel.DataAnnotations;

namespace Api.Data;

public class Module : BaseEntityWithId<ModuleId>
{
    [MaxLength(255)]
    public required string Name { get; set; }
    public required Uri RepoUri { get; set; }
}