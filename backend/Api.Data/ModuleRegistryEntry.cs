using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Auth.Abstractions;

namespace Api.Data;

public class ModuleRegistryEntry : BaseEntityWithId<ModuleRegistryEntryId>
{
    [MaxLength(255)]
    public required string Name { get; set; }

    [MaxLength(255)]
    public required string Organization { get; set; }

    [MaxLength(2000)]
    public string? Description { get; set; }

    public required Uri RepoUrl { get; set; }

    [MaxLength(50)]
    public string? LatestVersion { get; set; }

    public string[] Tags { get; set; } = [];

    public int InstallCount { get; set; }

    [Column(TypeName = "jsonb")]
    public Dictionary<string, string>? Compatibility { get; set; }

    [MaxLength(500)]
    public string? IconUrl { get; set; }

    public UserId PublishedByUserId { get; set; }

    public List<ModuleVersionEntry> Versions { get; set; } = [];
}
