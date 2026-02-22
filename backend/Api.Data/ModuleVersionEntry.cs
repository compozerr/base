using System.ComponentModel.DataAnnotations;

namespace Api.Data;

public class ModuleVersionEntry : BaseEntityWithId<ModuleVersionId>
{
    public ModuleRegistryEntryId ModuleRegistryEntryId { get; set; }

    [MaxLength(50)]
    public required string Version { get; set; }

    [MaxLength(40)]
    public required string CommitHash { get; set; }

    public string? ReleaseNotes { get; set; }

    public ModuleRegistryEntry ModuleRegistryEntry { get; set; } = null!;
}
